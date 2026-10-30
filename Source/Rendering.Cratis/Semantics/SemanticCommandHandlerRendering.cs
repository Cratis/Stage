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
    internal static string Method(SemanticCommand command, SemanticApplicationContext context) => context.InlineNames.HandlerBody(command);
    internal static string ContextType(SemanticCommand command, SemanticApplicationContext context) => context.InlineNames.HandlerContext(command);
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
        builder.OpenBlock($"private static {ReturnType(command, context)} {Method(command, context)}({ContextType(command, context)} context)")
            .Inline(context.RetainInlineInput(inline), inline.Placement, reportDegradation: diagnostic => context.InlineDiagnostics.Add(new(diagnostic.Code, Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Message, command.Id)))
            .EndBlock().BlankLine();
    }

    internal static void RenderContext(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        string PropertyType(SemanticProperty property) => types.Type(property.Type, reducerInput: true);
        var parameters = string.Join(", ", command.Properties.Select(property => $"{PropertyType(property)} {Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))}"));
        builder.OpenBlock($"private sealed class {SnapshotType(command, context)}({parameters})");
        foreach (var property in command.Properties)
        {
            builder.Line($"public {PropertyType(property)} {context.MemberNames.Property(property)} {{ get; }} = {Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))};");
        }
        builder.EndBlock().BlankLine()
            .OpenBlock($"private sealed class {ContextType(command, context)}({SnapshotType(command, context)} command)")
            .Line($"public {SnapshotType(command, context)} Command {{ get; }} = command;")
            .EndBlock().BlankLine();
    }

    internal static void Prepare(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals)
    {
        if (command.Handler is null) return;
        builder.Line($"var {locals.Name("handlerPayloads")} = {Call(command, context)};");
    }

    /// <summary>Renders the call that runs the authored handler body against the command snapshot.</summary>
    /// <param name="command">Command owning the payload handler.</param>
    /// <param name="context">Semantic application and generated type context.</param>
    /// <returns>The call expression.</returns>
    internal static string Call(SemanticCommand command, SemanticApplicationContext context)
    {
        var values = command.Properties.Select(property =>
        {
            var value = SemanticCommandResultRendering.PropertyValue(property, context);
            if (!property.Type.IsCollection) return value;
            var snapshot = $"global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({value})";
            return property.Type.IsOptional ? $"{value} is null ? null : {snapshot}" : snapshot;
        });
        return $"{Method(command, context)}(new {ContextType(command, context)}(new {SnapshotType(command, context)}({string.Join(", ", values)})))";
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

    static string SnapshotType(SemanticCommand command, SemanticApplicationContext context) => context.InlineNames.HandlerCommand(command);
}
