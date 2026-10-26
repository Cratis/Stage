// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Owns the typed, once-per-Handle command payload boundary.</summary>
internal static class SemanticCommandHandlerRendering
{
    internal static string Suffix(SemanticCommand command) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Handler!.RequirementId)))[..12];
    internal static string Method(SemanticCommand command) => $"HandlerBody_{Suffix(command)}";
    internal static string ContextType(SemanticCommand command) => $"HandlerContext_{Suffix(command)}";
    internal static string ReturnType(SemanticCommand command, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var events = command.Produces.Select(produced => types.EventType(context.Events[produced.EventContract])).ToArray();
        return events.Length == 1 ? events[0] : $"({string.Join(", ", events)})";
    }

    internal static void Render(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context)
    {
        if (command.Handler is null) return;
        RenderContext(builder, command, context);
        var inline = SemanticInlineCodeRendering.CommandBody(command, context);
        if (inline.Refused) throw UnsupportedSemanticRendering.For("command handler", string.Join(Environment.NewLine, inline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        builder.OpenBlock($"private static {ReturnType(command, context)} {Method(command)}({ContextType(command)} context)")
            .Inline(inline.Code!, inline.Placement, reportDegradation: diagnostic => context.InlineDiagnostics.Add(new(diagnostic.Code, Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Message, command.Id)))
            .EndBlock().BlankLine();
    }

    internal static void RenderContext(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        string PropertyType(SemanticProperty property) => types.Type(property.Type, reducerInput: true);
        var parameters = string.Join(", ", command.Properties.Select(property => $"{PropertyType(property)} {Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))}"));
        builder.OpenBlock($"private sealed class {SnapshotType(command)}({parameters})");
        foreach (var property in command.Properties)
        {
            builder.Line($"public {PropertyType(property)} {context.MemberNames.Property(property)} {{ get; }} = {Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))};");
        }
        builder.EndBlock().BlankLine()
            .OpenBlock($"private sealed class {ContextType(command)}({SnapshotType(command)} command)")
            .Line($"public {SnapshotType(command)} Command {{ get; }} = command;")
            .EndBlock().BlankLine();
    }

    internal static void Prepare(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals)
    {
        if (command.Handler is null) return;
        var values = command.Properties.Select(property =>
        {
            var value = SemanticCommandResultRendering.PropertyValue(property, context);
            if (!property.Type.IsCollection) return value;
            var snapshot = $"global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({value})";
            return property.Type.IsOptional ? $"{value} is null ? null : {snapshot}" : snapshot;
        });
        builder.Line($"var {locals.Name("handlerPayloads")} = {Method(command)}(new {ContextType(command)}(new {SnapshotType(command)}({string.Join(", ", values)})));");
    }

    internal static void RenderFastHandle(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals, bool bare, Func<SemanticProducedEvent, string> eventValue, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var single = command.Produces.Length == 1;
        var resultType = single ? "global::Cratis.Chronicle.EventSequences.EventForEventSourceId" : "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";
        if (bare) resultType = new SemanticTypeSystem(context).EventType(context.Events[command.Produces[0].EventContract]);
        builder.OpenBlock($"public {resultType} Handle()");
        Prepare(builder, command, context, locals);
        var payload = single ? wrappedEvent(command.Produces[0]) : $"[{string.Join(", ", command.Produces.Select(wrappedEvent))}]";
        if (bare) payload = eventValue(command.Produces[0]);
        builder.Line($"return {payload};").EndBlock();
    }

    internal static string Payload(SemanticCommand command, SemanticProducedEvent produced, SemanticHandleLocals locals)
    {
        var value = locals.Name("handlerPayloads");
        if (command.Produces.Length == 1) return value;

        // Productions have positional meaning even when their event type and mappings are equal.
        var index = Enumerable.Range(0, command.Produces.Length).Single(index => ReferenceEquals(command.Produces[index], produced));
        while (index >= 7)
        {
            value += ".Rest";
            index -= 7;
        }
        return $"{value}.Item{index + 1}";
    }

    static string SnapshotType(SemanticCommand command) => $"HandlerCommand_{Suffix(command)}";
}
