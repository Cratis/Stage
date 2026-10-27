// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders namespace-bound occurrence productions and separately receipted command invocations.
/// </summary>
internal static class SemanticOccurrenceHandlerArtifactRenderer
{
    internal static IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        var trigger = SemanticOccurrenceAdmission.Trigger(reaction, context);
        yield return SemanticTriggerArtifactRenderer.Render(located, trigger, context);
        var name = Identifiers.ToPascalCase(reaction.Name);
        var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        var types = new SemanticTypeSystem(context);
        var reactionTrigger = reaction.Triggers[0];
        var parameters = new List<string>();
        if (!reactionTrigger.Produces.IsEmpty)
            parameters.Add("global::Cratis.Chronicle.IChronicleClient client, global::Cratis.Chronicle.IEventStore eventStore");
        if (!reactionTrigger.Invokes.IsEmpty)
            parameters.Add("global::Cratis.Arc.Chronicle.Occurrences.IOccurrenceCommandExecutor commands");
        context.Docs(reaction.Id).Render(builder)
            .OpenBlock($"public class {name}({string.Join(", ", parameters)}) : global::Cratis.Chronicle.Occurrences.IOccurrenceHandler<{Identifiers.ToPascalCase(trigger.Name)}>")
            .Line("/// <inheritdoc/>")
            .OpenBlock($"public async global::System.Threading.Tasks.Task Handle({Identifiers.ToPascalCase(trigger.Name)} occurrence, global::Cratis.Chronicle.Occurrences.OccurrenceContext context, global::System.Threading.CancellationToken cancellationToken)");
        var scope = new RenderingScope(trigger, context, types);
        if (!reactionTrigger.Produces.IsEmpty) RenderProductionEffect(reactionTrigger, reaction, scope, builder);
        RenderInvocations(reactionTrigger, reaction, scope, builder);
        builder.EndBlock().EndBlock();
        yield return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [reaction.Id, trigger.Id] };
    }

    static void RenderProductionEffect(SemanticReactionTrigger reactionTrigger, SemanticReaction reaction, RenderingScope scope, CSharpCodeBuilder builder)
    {
        builder.Line("var store = context.Namespace is { } @namespace ? await client.GetEventStore(eventStore.Name, @namespace) : eventStore;")
            .Line($"var effect = context.EffectKey({CSharpCodeBuilder.StringLiteral($"stage:reaction:{reaction.Id}:productions")});");
        if (reactionTrigger.Invokes.IsEmpty) builder.Line("if (await store.Occurrences.GetEffectReceipt(effect) is not null) return;");
        else builder.OpenBlock("if (await store.Occurrences.GetEffectReceipt(effect) is null)");
        builder.Line("var unitOfWork = store.UnitOfWorkManager.Begin(global::Cratis.Execution.CorrelationId.New());")
            .OpenBlock("try")
            .Line("unitOfWork.UseEffectReceipt(effect, null);");
        RenderProductions(reactionTrigger, scope, builder)
            .Line("await unitOfWork.Commit();")
            .Line("if (!unitOfWork.IsSuccess) throw new global::System.InvalidOperationException(\"The occurrence effect could not be committed.\");")
            .EndBlock()
            .OpenBlock("catch")
            .Line("if (!unitOfWork.IsCompleted) await unitOfWork.Rollback();")
            .Line("throw;").EndBlock();
        if (!reactionTrigger.Invokes.IsEmpty) builder.EndBlock();
    }

    static void RenderInvocations(SemanticReactionTrigger trigger, SemanticReaction reaction, RenderingScope scope, CSharpCodeBuilder builder)
    {
        for (var index = 0; index < trigger.Invokes.Length; index++)
        {
            var invocation = trigger.Invokes[index];
            var command = scope.Context.Commands[invocation.Command];
            var arguments = command.Properties.Where(property => !property.IsGenerated).Select(property =>
                invocation.Mappings.FirstOrDefault(mapping => mapping.TargetProperty == property.Id) is { } mapping
                    ? Expression(mapping.Source, property.Type, scope)
                    : "null");
            var effect = CSharpCodeBuilder.StringLiteral($"stage:reaction:{reaction.Id}:invocation:{index}:{command.Id}");
            var roles = reaction.RunsAs is { } identity ? $"[{string.Join(", ", identity.Roles.Select(CSharpCodeBuilder.StringLiteral))}], " : string.Empty;
            var failure = CSharpCodeBuilder.StringLiteral($"Reaction '{reaction.Name}' command '{command.Name}' from application trigger '{scope.Trigger.Name}' did not settle.");
            var refusal = CSharpCodeBuilder.StringLiteral($"Reaction '{reaction.Name}' command '{command.Name}' from application trigger '{scope.Trigger.Name}' was refused.");
            builder.Line($"var invocation{index} = await commands.Execute(new {scope.Types.SliceType(command.Id, command.Name)}({string.Join(", ", arguments)}), context, {effect}, {roles}cancellationToken);")
                .Line($"if (!invocation{index}.IsSettled) throw new global::System.InvalidOperationException({failure});")
                .Line($"if (invocation{index}.Outcome is global::Cratis.Chronicle.Occurrences.Refused) throw new global::System.InvalidOperationException({refusal});");
        }
    }

    static CSharpCodeBuilder RenderProductions(SemanticReactionTrigger reactionTrigger, RenderingScope scope, CSharpCodeBuilder builder)
    {
        builder.OpenBlock("unitOfWork.AddEvents(global::Cratis.Chronicle.EventSequences.EventSequenceId.Log, new global::Cratis.Chronicle.EventSequences.EventForEventSourceId[]");
        foreach (var produced in reactionTrigger.Produces)
        {
            var target = scope.Context.Events[produced.EventContract];
            var arguments = target.Properties.Select(property => Argument(produced, target, property, scope));
            var destination = scope.Types.EventSourceExpression(Expression(produced.Destination!, produced.DestinationType!, scope), produced.DestinationType!);
            var tags = target.Tags.Concat(produced.Tags).ToArray();
            var suffix = tags.Length == 0 ? string.Empty : $" {{ Tags = [{string.Join(", ", tags.Select(CSharpCodeBuilder.StringLiteral))}] }}";
            builder.Line($"new({destination}, new {scope.Types.EventType(target)}({string.Join(", ", arguments)})){suffix},");
        }
        return builder.EndBlock().Line(", []);");
    }

    static string Argument(SemanticProducedEvent produced, SemanticEventContract target, SemanticProperty property, RenderingScope scope)
    {
        var source = produced.Mappings.Single(mapping => mapping.TargetProperty == property.Id).Source;
        var expression = Expression(source, property.Type, scope);
        return property.Type.IsCollection && source is SemanticResolvedExpression && scope.Context.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == target.Id))
            ? $"global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({expression})"
            : expression;
    }

    static string Expression(SemanticExpression expression, SemanticTypeReference type, RenderingScope scope) => expression switch
    {
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Trigger, Source: SemanticExpressionSourceKind.Property } resolved => $"occurrence.{Identifiers.ToPascalCase(scope.Trigger.Properties.Single(property => property.Id == resolved.Target).Name)}",
        SemanticValueExpression value => scope.Types.Value(value.Value, type),
        _ => throw UnsupportedSemanticRendering.For("occurrence expression", expression)
    };

    sealed record RenderingScope(SemanticApplicationTrigger Trigger, SemanticApplicationContext Context, SemanticTypeSystem Types);
}
