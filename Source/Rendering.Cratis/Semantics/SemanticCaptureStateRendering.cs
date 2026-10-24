// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the per-key capture state an inbox reactor rebuilds from the inbox prefix before the delivered event.
/// </summary>
internal static class SemanticCaptureStateRendering
{
    const string Record = "global::System.Collections.Generic.Dictionary<string, object?>";
    const string Invariant = "global::System.Globalization.CultureInfo.InvariantCulture";

    /// <summary>Renders an admitted guard over the reactor's previous and current capture state.</summary>
    /// <param name="when">The guard, or null for an unguarded append.</param>
    /// <returns>The C# condition, or null when the append is unguarded.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The guard was not admitted.</exception>
    internal static string? Condition(SemanticCaptureCondition? when) => when?.Kind switch
    {
        null => null,
        SemanticCaptureConditionKind.Added => "previous is null",
        SemanticCaptureConditionKind.AnyChanged => $"previous is not null && ({Changes(when.Fields, " || ", "false")})",
        SemanticCaptureConditionKind.AllChanged => $"previous is not null && ({Changes(when.Fields, " && ", "true")})",
        SemanticCaptureConditionKind.Transition =>
            $"previous is not null && Text(previous.GetValueOrDefault({Literal(when.Fields[0])})) == {Literal(when.From)} && Text(current.GetValueOrDefault({Literal(when.Fields[0])})) == {Literal(when.To)}",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCaptureCondition), when.Kind.ToString())
    };

    /// <summary>Renders the state members of a capture reactor.</summary>
    /// <param name="builder">The builder positioned inside the reactor class.</param>
    /// <param name="capture">The capture.</param>
    /// <param name="name">The reactor type name.</param>
    /// <param name="sources">The captured foreign event contracts.</param>
    /// <param name="types">The type system.</param>
    internal static void Render(CSharpCodeBuilder builder, SemanticCapture capture, string name, IReadOnlyList<SemanticEventContract> sources, SemanticTypeSystem types)
    {
        var eventTypes = string.Join(", ", sources.Select(source => $"eventStore.EventTypes.GetEventTypeFor(typeof({types.EventType(source)}))"));
        RenderPrevious(builder, capture, name, eventTypes);
        RenderObserve(builder, sources, types);
        builder.OpenBlock($"static {Record} Overlay({Record}? previous, {Record} observed)")
            .Line($"var current = previous is null ? new {Record}() : new {Record}(previous);")
            .Line("foreach (var field in observed) current[field.Key] = field.Value;")
            .Line("return current;")
            .EndBlock().BlankLine()
            .Line($"static bool SameState({Record} previous, {Record} current) =>")
            .Line("    previous.Count == current.Count && current.All(field => previous.TryGetValue(field.Key, out var earlier) && Same(earlier, field.Value));")
            .BlankLine()
            .Line($"static bool Changed({Record} previous, {Record} current, string field) =>")
            .Line("    previous.ContainsKey(field) != current.ContainsKey(field) ||")
            .Line("    (previous.ContainsKey(field) && !Same(previous.GetValueOrDefault(field), current.GetValueOrDefault(field)));")
            .BlankLine()
            .Line("static string Text(object? value) => (value is null ? null : ConceptValue(value) ?? value) switch")
            .Line("{")
            .Line("    null => string.Empty,")
            .Line("    string text => text,")
            .Line("    bool flag => flag ? \"true\" : \"false\",")
            .Line($"    global::System.IConvertible number => global::System.Convert.ToDecimal(number, {Invariant}).ToString(\"0.############################\", {Invariant}),")
            .Line("    _ => string.Empty")
            .Line("};")
            .BlankLine()
            .OpenBlock("static object? ConceptValue(object value)")
            .OpenBlock("for (var type = value.GetType(); type is not null; type = type.BaseType)")
            .Line("if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(global::Cratis.Concepts.ConceptAs<>)) return type.GetProperty(\"Value\")!.GetValue(value);")
            .EndBlock()
            .Line("return null;")
            .EndBlock().BlankLine()
            .Line($"static T Field<T>({Record} state, string field, bool required) =>")
            .Line("    state.GetValueOrDefault(field) is T value ? value : required ? throw new MissingCaptureField(field) : default!;")
            .BlankLine()
            .Line($"sealed class MissingCaptureField(string field) : global::System.Exception({Literal($"Capture '{capture.Name}' has no value for field '")} + field + \"' in the state remembered for this key.\");")
            .Line($"sealed class EmptyCaptureKey() : global::System.Exception({Literal($"Capture '{capture.Name}' received an event with an empty key '{capture.Key}'.")});")
            .BlankLine();
        if (capture.EventsSource!.KeyIsEventSource)
        {
            builder.Line($"sealed class CaptureKeyIsNotTheEventSource() : global::System.Exception({Literal($"Capture '{capture.Name}' key '{capture.Key}' does not match the delivered event source.")});").BlankLine();
        }
        SemanticPublicationArtifactRenderer.RenderComparison(builder, "Same");
    }

    static void RenderPrevious(CSharpCodeBuilder builder, SemanticCapture capture, string name, string eventTypes)
    {
        var key = Literal(capture.Key);
        var keyIsEventSource = capture.EventsSource!.KeyIsEventSource;
        builder.OpenBlock($"async global::System.Threading.Tasks.Task<{Record}?> Previous({Record} observed, global::Cratis.Chronicle.Events.EventContext context)")
            .Line($"var key = observed.GetValueOrDefault({key});")
            .Line("if (key is null || (ConceptValue(key) ?? key) is string text && string.IsNullOrWhiteSpace(text)) throw new EmptyCaptureKey();");
        if (keyIsEventSource)
        {
            builder.Line($"if (!global::System.String.Equals(global::System.Convert.ToString(ConceptValue(key) ?? key, {Invariant}), context.EventSourceId.Value, global::System.StringComparison.Ordinal)) throw new CaptureKeyIsNotTheEventSource();");
        }
        var sourceFilter = keyIsEventSource ? "eventSourceId: context.EventSourceId, " : string.Empty;
        builder.Line($"var inbox = eventStore.GetEventSequence(global::Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof({name})));")
            .Line($"var history = await inbox.GetFromSequenceNumber(global::Cratis.Chronicle.Events.EventSequenceNumber.First, {sourceFilter}filterEventTypes: [{eventTypes}]);")
            .Line($"{Record}? previous = null;")
            .OpenBlock("foreach (var fact in history.Where(fact => fact.Context.SequenceNumber.Value < context.SequenceNumber.Value).OrderBy(fact => fact.Context.SequenceNumber.Value))")
            .Line($"if (Observe(fact.Content) is {{ }} fields && Same(fields.GetValueOrDefault({key}), key)) previous = Overlay(previous, fields);")
            .EndBlock()
            .Line("return previous;")
            .EndBlock().BlankLine();
    }

    static void RenderObserve(CSharpCodeBuilder builder, IEnumerable<SemanticEventContract> sources, SemanticTypeSystem types)
    {
        builder.Line($"static {Record}? Observe(object content) => content switch").Line("{");
        foreach (var source in sources)
        {
            var fields = source.Properties.Select(property => $"[{Literal(property.Name)}] = @event.{Identifiers.ToPascalCase(property.Name)}");
            builder.Line($"    {types.EventType(source)} @event => new() {{ {string.Join(", ", fields)} }},");
        }
        builder.Line("    _ => null").Line("};").BlankLine();
    }

    static string Changes(System.Collections.Immutable.ImmutableArray<string> fields, string separator, string empty) =>
        fields.IsDefaultOrEmpty ? empty : string.Join(separator, fields.Select(field => $"Changed(previous, current, {Literal(field)})"));

    static string Literal(string? value) => value is null ? "null" : CSharpCodeBuilder.StringLiteral(value);
}
