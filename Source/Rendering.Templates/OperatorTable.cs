// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal sealed record OperatorTable(string Open, string Close, ImmutableDictionary<string, OperatorTable.Row> Rows)
{
    internal static readonly string[] Names = ["and", "or", "equal", "notEqual", "greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual"];

    internal static OperatorTable Parse(ReadOnlySpan<byte> bytes)
    {
        try
        {
            if (bytes.Length > TemplateProfile.MaxTemplateBytes) throw JsonScriptValues.Invalid("Operator table exceeds 65536 bytes.");
            using var document = JsonDocument.Parse(bytes.ToArray());
            var root = document.RootElement;
            Fields(root, ["open", "close", "operators"]);
            var entries = root.GetProperty("operators");
            Fields(entries, Names);
            var rows = ImmutableDictionary.CreateBuilder<string, Row>(StringComparer.Ordinal);
            foreach (var entry in entries.EnumerateObject())
            {
                Fields(entry.Value, ["text", "precedence"]);
                if (!entry.Value.GetProperty("precedence").TryGetInt32(out var precedence) || precedence is < 1 or > 100) throw JsonScriptValues.Invalid("Precedence must be 1..100.");
                rows.Add(entry.Name, new(Text(entry.Value, "text"), precedence));
            }

            return new(Text(root, "open"), Text(root, "close"), rows.ToImmutable());
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or TemplateRefusal)
        {
            throw new TemplateRefusal(new(TemplateCodes.OperatorTable, "Invalid operators table: " + error.Message, string.Empty, 0, 0));
        }
    }

    internal static void Fields(JsonElement value, string[] names, bool optional = false)
    {
        if (value.ValueKind != JsonValueKind.Object) throw JsonScriptValues.Invalid("Expected a JSON object.");
        var properties = value.EnumerateObject().Select(property => property.Name).ToArray();
        if ((!optional && properties.Length != names.Length) || properties.Distinct(StringComparer.Ordinal).Count() != properties.Length || properties.Any(property => !names.Contains(property, StringComparer.Ordinal))) throw JsonScriptValues.Invalid("Unknown, duplicate or missing JSON field.");
    }

    internal static string Text(JsonElement value, string field)
    {
        var text = value.GetProperty(field).GetString();
        if (string.IsNullOrEmpty(text) || text.Contains('\n') || text.Contains('\r') || TemplatePreScan.HasPrivateUse(text)) throw JsonScriptValues.Invalid("Expected nonempty single-line ordinary text.");
        return text;
    }

    internal sealed record Row(string Text, int Precedence);
}
