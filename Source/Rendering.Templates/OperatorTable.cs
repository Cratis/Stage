// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class OperatorTable(ImmutableDictionary<string, OperatorTable.Row> rows)
{
    internal static OperatorTable Empty { get; } = new([]);

    internal static OperatorTable Parse(ReadOnlySpan<byte> bytes)
    {
        try
        {
            if (bytes.Length > TemplatePreScan.MaxBytes) throw JsonScriptValues.Invalid("Operator table exceeds 65536 bytes.");
            using var document = JsonDocument.Parse(bytes.ToArray());
            Fields(document.RootElement, ["operators"]);
            var entries = document.RootElement.GetProperty("operators");
            if (entries.ValueKind != JsonValueKind.Array) throw JsonScriptValues.Invalid("operators must be an array.");
            var rows = ImmutableDictionary.CreateBuilder<string, Row>(StringComparer.Ordinal);
            foreach (var entry in entries.EnumerateArray())
            {
                Fields(entry, ["operator", "position", "token", "precedence", "associativity"]);
                var name = Text(entry, "operator");
                var position = Text(entry, "position");
                var token = Text(entry, "token");
                var associativity = Text(entry, "associativity");
                if (position is not ("unary" or "binary") || associativity is not ("left" or "right" or "none") || !entry.GetProperty("precedence").TryGetInt32(out var precedence) || precedence is < 0 or > 1000 || token.Any(char.IsControl)) throw JsonScriptValues.Invalid("Invalid operator row.");
                if (!rows.TryAdd(position + ":" + name, new(name, position, token, precedence, associativity))) throw JsonScriptValues.Invalid("Duplicate operator row.");
            }

            return new(rows.ToImmutable());
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or TemplateRefusal)
        {
            throw new TemplateRefusal(new(TemplateCodes.OperatorTable, "Invalid operators table: " + error.Message, string.Empty, 0, 0));
        }
    }

    internal static void Fields(JsonElement value, string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object) throw JsonScriptValues.Invalid("Expected a JSON object.");
        var properties = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (properties.Length != names.Length || properties.Distinct(StringComparer.Ordinal).Count() != properties.Length || properties.Any(property => !names.Contains(property, StringComparer.Ordinal))) throw JsonScriptValues.Invalid("Unknown, duplicate or missing JSON field.");
    }

    internal static string Text(JsonElement value, string field)
    {
        var text = value.GetProperty(field).GetString();
        if (string.IsNullOrWhiteSpace(text) || TemplatePreScan.HasPrivateUse(text)) throw JsonScriptValues.Invalid("Expected nonempty ordinary text.");
        return text;
    }

    internal bool TryGet(string position, string name, out Row row) => rows.TryGetValue(position + ":" + name, out row!);

    internal sealed record Row(string Name, string Position, string Token, int Precedence, string Associativity);
}
