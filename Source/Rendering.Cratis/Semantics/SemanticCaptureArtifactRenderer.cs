// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Lowers a foreign events-source capture to an inbox reactor producing local private facts.
/// </summary>
internal static class SemanticCaptureArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticCapture capture, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var name = Identifiers.ToPascalCase(capture.Name);
        var source = capture.EventsSource!;
        var added = capture.Appends.Any(append => append.When is not null);
        var store = context.Events[source.Events[0]].Origin!;
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        if (added) builder.Using("System.Linq");
        context.Docs(capture.Id).Render(builder)
            .Attribute($"global::Cratis.Chronicle.Events.EventStoreAttribute({CSharpCodeBuilder.StringLiteral(store)})")
            .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral(capture.Id.ToString())})")
            .OpenBlock($"public class {name}{(added ? "(global::Cratis.Chronicle.IEventStore eventStore)" : string.Empty)} : global::Cratis.Chronicle.Reactors.IReactor");
        foreach (var id in source.Events)
        {
            var contract = context.Events[id];
            const string result = "global::System.Collections.Generic.IEnumerable<object>";
            var signature = added ? $"async global::System.Threading.Tasks.Task<{result}>" : result;
            builder.OpenBlock($"public {signature} On{Identifiers.ToPascalCase(contract.Name)}({types.EventType(contract)} @event)")
                .Line("var effects = new global::System.Collections.Generic.List<object>();");
            foreach (var append in capture.Appends)
            {
                RenderAppend(builder, capture, append, contract, context, types);
            }
            builder.Line("return effects;").EndBlock().BlankLine();
        }
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString()) { Sources = [capture.Id] };
    }

    static void RenderAppend(CSharpCodeBuilder builder, SemanticCapture capture, SemanticCaptureAppend append, SemanticEventContract source, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var contract = context.Events[append.EventContract];
        var key = source.Properties.Single(property => property.Name == capture.Key);
        var destination = types.EventSourceExpression($"@event.{Identifiers.ToPascalCase(key.Name)}", key.Type);
        if (append.When is not null)
        {
            // An existing local fact approximates capture 'added' state; this is not persistent capture-engine state.
            builder.Line("// 'added' is approximated by an existing local fact for this key and target event type.")
                .OpenBlock($"if (!(await eventStore.EventLog.GetForEventSourceIdAndEventTypes({destination}, [eventStore.EventTypes.GetEventTypeFor(typeof({types.EventType(contract)}))])).Any())");
        }
        var arguments = contract.Properties.Select(property => Mapping(append, property, source, types));
        var tags = contract.Tags.Concat(append.Tags).Select(CSharpCodeBuilder.StringLiteral).ToArray();
        var metadata = tags.Length == 0 ? string.Empty : $" {{ Tags = [{string.Join(", ", tags)}] }}";
        builder.Line($"effects.Add(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId({destination}, new {types.EventType(contract)}({string.Join(", ", arguments)})){metadata});");
        if (append.When is not null) builder.EndBlock();
    }

    static string Mapping(SemanticCaptureAppend append, SemanticProperty property, SemanticEventContract source, SemanticTypeSystem types) =>
        append.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == property.Id) switch
        {
            { Field: { } field } => $"@event.{Identifiers.ToPascalCase(source.Properties.Single(member => member.Name == field).Name)}",
            { Value: SemanticValueExpression value } => types.Value(value.Value, property.Type),
            null when property.Type.IsOptional => "null",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCaptureMapping), property.Name)
        };
}
