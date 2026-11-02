// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders event reactions as rename-stable Chronicle reactors with explicit append destinations.
/// </summary>
internal static class SemanticReactorArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        if (reaction.Triggers.Any(trigger => trigger.RequirementId is not null)) return RenderAuthored(located, reaction, context);
        var types = new SemanticTypeSystem(context);
        var name = Identifiers.ToPascalCase(reaction.Name);
        var builder = ReactorBuilder(located, reaction, context);

        // For atomic models, admission permits at most one production trigger per source event:
        // this returned collection is exactly that trigger occurrence's append batch. Legacy output stays unchanged.
        foreach (var group in reaction.Triggers.GroupBy(trigger => trigger.Source))
        {
            var source = context.Events[group.Key];
            builder.BlankLine()
                .Summary($"Reacts to {Identifiers.ToWords(source.Name)}.")
                .Parameter("event", "The triggering event.")
                .Parameter("context", "The triggering event context.")
                .Returns("The modeled reaction effects.")
                .OpenBlock($"public global::System.Collections.Generic.IEnumerable<object> {Identifiers.ToPascalCase(source.Name)}({types.EventType(source)} @event, global::Cratis.Chronicle.Events.EventContext context)")
                .Line("var effects = new global::System.Collections.Generic.List<object>();");
            foreach (var trigger in group)
            {
                if (trigger.Where is { } condition)
                {
                    builder.OpenBlock($"if ({SemanticConditionRendering.Render(condition, new(source.Properties, "@event", context))})");
                }
                foreach (var produced in trigger.Produces)
                {
                    builder.Line($"effects.Add({Production(produced, source, types, context)});");
                }
                foreach (var invocation in trigger.Invokes)
                {
                    var command = context.Commands[invocation.Command];
                    var arguments = command.Properties.Where(property => !property.IsGenerated).Select(property =>
                        invocation.Mappings.FirstOrDefault(mapping => mapping.TargetProperty == property.Id) is { } mapping
                            ? Expression(mapping.Source, property.Type, source, types)
                            : "null");
                    var effect = $"new {types.SliceType(command.Id, command.Name)}({string.Join(", ", arguments)})";
                    if (!invocation.OnRefused.IsEmpty)
                    {
                        effect = $"global::Cratis.Arc.Chronicle.Reactors.Invoke.Command({effect})";
                        foreach (var branch in invocation.OnRefused)
                        {
                            var selector = Selector(branch);
                            effect += branch.Produces.IsEmpty
                                ? $".Acknowledge({selector})"
                                : $".OnRefused({selector}, refusal => new object[] {{ {string.Join(", ", branch.Produces.Select(produced => Production(produced, source, types, context, $"{name}RefusalKindIsUnknown")))} }})";
                        }
                    }
                    builder.Line($"effects.Add({effect});");
                }
                if (trigger.Where is not null) builder.EndBlock();
            }
            builder.Line("return effects;").EndBlock();
        }
        if (reaction.Triggers.SelectMany(trigger => trigger.Invokes).SelectMany(invocation => invocation.OnRefused)
            .SelectMany(branch => branch.Produces).SelectMany(produced => produced.Mappings)
            .Any(mapping => mapping.Source is SemanticRefusalExpression refusal && refusal.Member == "reason"))
        {
            builder.BlankLine()
                .Line("/// <summary>")
                .Line("/// The exception that is thrown when an invocation returns an unknown refusal kind.")
                .Line("/// </summary>")
                .Line($"public sealed class {name}RefusalKindIsUnknown() : global::System.Exception(\"Unknown command refusal kind.\");");
        }
        builder.EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [reaction.Id] };
    }

    static CSharpCodeBuilder ReactorBuilder(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.Docs(reaction.Id).Render(builder, $"Reacts through {Identifiers.ToPascalCase(reaction.Name)}.")
            .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral(reaction.Id.ToString())})")
            .Attribute("global::Cratis.Chronicle.Reactors.OnceOnlyAttribute");
        RenderIdentityAttribute(builder, reaction);
        if (reaction.From is { } filter) SemanticObserverFilterRendering.Attribute(builder, filter, context);
        return builder.OpenBlock($"public class {Identifiers.ToPascalCase(reaction.Name)} : global::Cratis.Chronicle.Reactors.IReactor");
    }

    static RenderedFile RenderAuthored(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        if (reaction.Triggers.Length != 1 || !SemanticVersionFeatures.HasStandaloneEventBody(reaction, reaction.Triggers[0]))
        {
            throw UnsupportedSemanticRendering.For("reaction", reaction.Name);
        }

        var inline = SemanticInlineCodeRendering.Reaction(located, reaction, context);
        if (inline.Refused)
        {
            throw UnsupportedSemanticRendering.For("reaction body", string.Join(Environment.NewLine, inline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        var builder = ReactorBuilder(located, reaction, context);
        var source = context.Events[reaction.Triggers[0].Source];
        var types = new SemanticTypeSystem(context);
        builder.BlankLine()
            .Summary($"Reacts to {Identifiers.ToWords(source.Name)}.")
            .Parameter("event", "The triggering event.")
            .Parameter("context", "The triggering event context.")
            .Returns("The modeled reaction effects.")
            .OpenBlock($"public global::System.Collections.Generic.IEnumerable<object>? {Identifiers.ToPascalCase(source.Name)}({types.EventType(source)} @event, global::Cratis.Chronicle.Events.EventContext context)");
        foreach (var name in CSharpBodyBindings.Referenced(inline.Code!.Text, source.Properties.Select(property => Identifiers.ToPascalCase(property.Name))))
        {
            builder.Line($"var {name} = @event.{name};");
        }

        if (reaction.Triggers[0].Where is { } condition)
        {
            builder.Line($"if (!({SemanticConditionRendering.Render(condition, new(source.Properties, "@event", context))})) return null;");
        }

        builder.Inline(context.RetainInlineInput(inline), inline.Placement).EndBlock().EndBlock();
        var content = builder.ToString();
        if (SemanticReactionBodyAdmission.Failure(context, content) is { } failure)
        {
            throw UnsupportedSemanticRendering.For("reaction body", $"The reaction handler does not compile in its rendered reactor: {failure}");
        }

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{Identifiers.ToPascalCase(reaction.Name)}.cs"]), content) { Sources = [reaction.Id] };
    }

    static void RenderIdentityAttribute(CSharpCodeBuilder builder, SemanticReaction reaction)
    {
        if (reaction.RunsAs is not { } identity) return;
        var roles = string.Join(", ", identity.Roles.Select(CSharpCodeBuilder.StringLiteral));
        builder.Attribute(identity.Roles.IsEmpty
            ? "global::Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute"
            : $"global::Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute({roles})");
    }

    static string Production(SemanticProducedEvent produced, SemanticEventContract source, SemanticTypeSystem types, SemanticApplicationContext context, string? refusalException = null)
    {
        var target = context.Events[produced.EventContract];
        var arguments = target.Properties.Select(property =>
        {
            var mapping = produced.Mappings.Single(mapping => mapping.TargetProperty == property.Id);
            var expression = Expression(mapping.Source, property.Type, source, types, refusalException);
            if (RequiresImmutableReactionPayload(context, target, property, mapping))
            {
                expression = $"global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({expression})";
            }

            return expression;
        });
        var destination = produced.Destination is { } expression
            ? types.EventSourceExpression(Expression(expression, produced.DestinationType!, source, types), produced.DestinationType!)
            : "context.EventSourceId";
        var metadata = new List<string>();
        if (produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }))
        {
            metadata.Add("Occurred = context.Occurred");
        }
        var tags = target.Tags.Concat(produced.Tags).ToArray();
        if (tags.Length > 0) metadata.Add($"Tags = [{string.Join(", ", tags.Select(CSharpCodeBuilder.StringLiteral))}]");
        var envelope = $"new global::Cratis.Chronicle.EventSequences.EventForEventSourceId({destination}, new {types.EventType(target)}({string.Join(", ", arguments)}))";

        return metadata.Count == 0 ? envelope : $"{envelope} {{ {string.Join(", ", metadata)} }}";
    }

    static bool RequiresImmutableReactionPayload(SemanticApplicationContext context, SemanticEventContract target, SemanticProperty property, SemanticPropertyMapping mapping) =>
        property.Type.IsCollection && mapping.Source is SemanticResolvedExpression &&
        context.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == target.Id));

    static string Selector(SemanticInvocationRefusal branch) => $"global::Cratis.Arc.Chronicle.Reactors.Refused.{SelectorMember(branch)}";

    static string SelectorMember(SemanticInvocationRefusal branch) => branch.Selector switch
    {
        "any" => "Any",
        "validation" => "ByValidation",
        "authorization" => "ByAuthorization",
        "constraint" when branch.Constraint is { } name => $"ByConstraint({CSharpCodeBuilder.StringLiteral(name)})",
        "constraint" => "ByAnyConstraint",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticInvocationRefusal), branch.Selector)
    };

    static string RefusalMember(string member, string refusalException) => member switch
    {
        "reason" => $"(refusal.Kind switch {{ global::Cratis.Arc.Commands.CommandRefusalKind.Validation => \"validation\", global::Cratis.Arc.Commands.CommandRefusalKind.Constraint => \"constraint\", global::Cratis.Arc.Commands.CommandRefusalKind.Authorization => \"authorization\", _ => throw new {refusalException}() }})",
        "constraint" => "refusal.ConstraintName",
        "message" => "refusal.Message",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticRefusalExpression), member)
    };

    static string Expression(SemanticExpression expression, SemanticTypeReference target, SemanticEventContract source, SemanticTypeSystem types, string? refusalException = null) => expression switch
    {
        SemanticRefusalExpression refusal when refusalException is not null => RefusalMember(refusal.Member, refusalException),
        SemanticResolvedExpression resolved => $"@event.{Identifiers.ToPascalCase(source.Properties.Single(property => property.Id == resolved.Target).Name)}",
        SemanticValueExpression value => types.Value(value.Value, target),
        SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } => "context.Occurred",
        SemanticEventContextExpression { Value: SemanticEventContextValueKind.EventSourceIdentity } => types.EventSourceIdentityValue(target),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticExpression), expression.Kind)
    };
}
