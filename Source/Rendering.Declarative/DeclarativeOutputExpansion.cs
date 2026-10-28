// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one output of a kit expanded for one fan-out item.
/// </summary>
/// <param name="Output">The declared output.</param>
/// <param name="ItemId">The fan-out item identity, or <see langword="null"/> for a single output.</param>
/// <param name="Scope">The template scope.</param>
sealed record ExpandedOutput(KitOutput Output, string? ItemId, JsonElement Scope);

/// <summary>
/// Represents the expansion of every kit output, or the safety refusal that stopped it.
/// </summary>
/// <param name="Outputs">The expanded outputs in manifest order, then ordinal item identity.</param>
/// <param name="Refusal">The refusal, or <see langword="null"/> when expanded.</param>
sealed record DeclarativeOutputExpansion(ImmutableArray<ExpandedOutput> Outputs, ArtifactRenderDiagnostic? Refusal)
{
    internal static DeclarativeOutputExpansion Expand(TemplatePlanSession session, KitManifest manifest, JsonElement root)
    {
        var outputs = new List<ExpandedOutput>();
        foreach (var output in manifest.Outputs)
        {
            var refusal = ExpandOutput(session, output, root, outputs);
            if (refusal is not null)
            {
                return new([], refusal);
            }
        }

        return new([.. outputs], null);
    }

    internal static ArtifactRenderDiagnostic Refuse(string code, string message) => new(code, ArtifactRenderDiagnosticSeverity.Error, message, default);

    static ArtifactRenderDiagnostic? ExpandOutput(TemplatePlanSession session, KitOutput output, JsonElement root, List<ExpandedOutput> outputs)
    {
        var modelScope = Scope(root, null);
        var items = session.EvaluateForEach(output.Id, modelScope);
        if (items.Refusal is not null)
        {
            return Refuse(items.Refusal.Code, items.Refusal.Message);
        }

        if (output.ForEach is null)
        {
            return Include(session, output, null, modelScope, outputs);
        }

        var ordered = new List<(string Id, JsonElement Item)>();
        foreach (var item in items.Value)
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String)
            {
                return Refuse(DeclarativeCodes.ItemWithoutId, $"An item of output {output.Id} has no string id.");
            }

            ordered.Add((id.GetString()!, item));
        }

        foreach (var (id, item) in ordered.OrderBy(_ => _.Id, StringComparer.Ordinal))
        {
            var refusal = Include(session, output, id, Scope(root, item), outputs);
            if (refusal is not null)
            {
                return refusal;
            }
        }

        return null;
    }

    static ArtifactRenderDiagnostic? Include(TemplatePlanSession session, KitOutput output, string? itemId, JsonElement scope, List<ExpandedOutput> outputs)
    {
        var when = session.EvaluateWhen(output.Id, scope);
        if (when.Refusal is not null)
        {
            return Refuse(when.Refusal.Code, when.Refusal.Message);
        }

        if (when.Value)
        {
            outputs.Add(new(output, itemId, scope));
        }

        return null;
    }

    static JsonElement Scope(JsonElement root, JsonElement? item)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("model");
            root.WriteTo(writer);
            if (item is { } value)
            {
                writer.WritePropertyName("item");
                value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }
}
