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
        var types = new SemanticTypeSystem(context);
        var name = Identifiers.ToPascalCase(reaction.Name);
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.Docs(reaction.Id).Render(builder)
            .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral(reaction.Id.ToString())})")
            .Attribute("global::Cratis.Chronicle.Reactors.OnceOnlyAttribute")
            .OpenBlock($"public class {name} : global::Cratis.Chronicle.Reactors.IReactor");
        foreach (var group in reaction.Triggers.GroupBy(trigger => trigger.Source))
        {
            var source = context.Events[group.Key];
            builder.BlankLine()
                .OpenBlock($"public global::System.Collections.Generic.IEnumerable<object> {Identifiers.ToPascalCase(source.Name)}({types.EventType(source)} @event, global::Cratis.Chronicle.Events.EventContext context)")
                .Line("var effects = new global::System.Collections.Generic.List<object>();");
            foreach (var trigger in group)
            {
                if (trigger.Where is { } condition)
                {
                    builder.OpenBlock($"if ({SemanticConditionRendering.Render(condition, source.Properties, "@event", context)})");
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
                    builder.Line($"effects.Add(new {types.SliceType(command.Id, command.Name)}({string.Join(", ", arguments)}));");
                }
                if (trigger.Where is not null) builder.EndBlock();
            }
            builder.Line("return effects;").EndBlock();
        }
        builder.EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [reaction.Id] };
    }

    static string Production(SemanticProducedEvent produced, SemanticEventContract source, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var target = context.Events[produced.EventContract];
        var arguments = target.Properties.Select(property =>
        {
            var mapping = produced.Mappings.Single(mapping => mapping.TargetProperty == property.Id);
            var expression = Expression(mapping.Source, property.Type, source, types);
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

    static string Expression(SemanticExpression expression, SemanticTypeReference target, SemanticEventContract source, SemanticTypeSystem types) => expression switch
    {
        SemanticResolvedExpression resolved => $"@event.{Identifiers.ToPascalCase(source.Properties.Single(property => property.Id == resolved.Target).Name)}",
        SemanticValueExpression value => types.Value(value.Value, target),
        SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } => "context.Occurred",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticExpression), expression.Kind)
    };
}
