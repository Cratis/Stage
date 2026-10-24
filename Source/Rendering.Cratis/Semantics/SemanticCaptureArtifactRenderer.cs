// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Lowers a foreign events-source capture to an inbox reactor producing local private facts.
/// </summary>
/// <remarks>
/// The reactor rebuilds the key's previous capture state from the inbox prefix before the delivered event,
/// so first observation, unchanged suppression and change guards are decided as the reference evaluator decides them.
/// </remarks>
internal static class SemanticCaptureArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticCapture capture, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var plan = context.CaptureMembers(capture);
        var name = plan.TypeName;
        var sources = capture.EventsSource!.Events.Select(id => context.Events[id]).ToArray();
        var builder = new CSharpCodeBuilder().Using("System.Linq").Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.Docs(capture.Id).Render(builder)
            .Attribute($"global::Cratis.Chronicle.Events.EventStoreAttribute({CSharpCodeBuilder.StringLiteral(sources[0].Origin!)})")
            .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral(capture.Id.ToString())})")
            .Attribute("global::Cratis.Chronicle.Reactors.OnceOnlyAttribute")
            .OpenBlock($"public class {name}(global::Cratis.Chronicle.IEventStore {plan.Store}) : global::Cratis.Chronicle.Reactors.IReactor");
        foreach (var source in sources)
        {
            const string result = "global::System.Collections.Generic.IEnumerable<object>";
            builder.Line("// Each effect is keyed by its delivery; a kernel without idempotent appends refuses the key rather than appending twice.")
                .OpenBlock($"public async global::System.Threading.Tasks.Task<{result}> {plan.Handler(source.Id)}({types.EventType(source)} @event, global::Cratis.Chronicle.Events.EventContext context, global::Cratis.Chronicle.Reactors.ReactorDelivery delivery)")
                .Line("var effects = new global::System.Collections.Generic.List<object>();")
                .Line($"var observed = {plan["Observe"]}(@event)!;")
                .Line($"var previous = await {plan["Previous"]}(observed, context);")
                .Line($"var current = {plan["Overlay"]}(previous, observed);")
                .Line($"if (previous is not null && {plan["SameState"]}(previous, current)) return effects;");
            for (var index = 0; index < capture.Appends.Length; index++)
            {
                RenderAppend(builder, capture, capture.Appends[index], index, source, context, types);
            }
            builder.Line("return effects;").EndBlock().BlankLine();
        }
        SemanticCaptureStateRendering.Render(builder, capture, plan, sources, types);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [capture.Id] };
    }

    static void RenderAppend(CSharpCodeBuilder builder, SemanticCapture capture, SemanticCaptureAppend append, int index, SemanticEventContract source, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var contract = context.Events[append.EventContract];
        if (append.When is { Kind: SemanticCaptureConditionKind.Removed })
        {
            builder.Line($"// 'when removed' never holds for a capture over events: an event never removes its key, so {contract.Name} is not appended.");
            return;
        }
        var key = source.Properties.Single(property => property.Name == capture.Key);
        var identityType = SemanticCaptureIdentitySupport.UsesTypedIdentity(context.Request.Model.SemanticVersion) ? append.EventSourceType : key.Type;
        var destination = types.EventSourceExpression($"@event.{Identifiers.ToPascalCase(key.Name)}", identityType);
        var guard = SemanticCaptureStateRendering.Condition(append.When, context.CaptureMembers(capture));
        if (guard is not null) builder.OpenBlock($"if ({guard})");
        var arguments = contract.Properties.Select(property => Mapping(append, property, types, context.CaptureMembers(capture)));
        var tags = contract.Tags.Concat(append.Tags).Select(CSharpCodeBuilder.StringLiteral).ToArray();
        var metadata = $" {{ IdempotencyKey = delivery.IdempotencyKeyFor({CSharpCodeBuilder.StringLiteral($"{capture.Id}/{index}")}){(tags.Length == 0 ? string.Empty : $", Tags = [{string.Join(", ", tags)}]")} }}";
        builder.Line($"effects.Add(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId({destination}, new {types.EventType(contract)}({string.Join(", ", arguments)})){metadata});");
        if (guard is not null) builder.EndBlock();
    }

    static string Mapping(SemanticCaptureAppend append, SemanticProperty property, SemanticTypeSystem types, SemanticCaptureMemberPlan plan) =>
        append.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == property.Id) switch
        {
            { Field: { } field } => $"{plan["Field"]}<{types.Type(property.Type)}>(current, {CSharpCodeBuilder.StringLiteral(field)}, required: {(property.Type.IsOptional ? "false" : "true")})",
            { Value: SemanticValueExpression value } => types.Value(value.Value, property.Type),
            null when property.Type.IsOptional => "null",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCaptureMapping), property.Name)
        };
}
