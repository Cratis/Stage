// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class ExpressionLowerer(OperatorTable table, ITemplateLanguage language, PlanMeter meter, int recursion)
{
    internal static JsonElement Input(object? value) => value is JsonScriptValues.JsonObject expression ? expression.Source : throw JsonScriptValues.Invalid("Expected an expression JSON object.");

    internal static string Kind(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty("$kind", out var kind) && kind.ValueKind == JsonValueKind.String ? kind.GetString()! : throw JsonScriptValues.Invalid("Expression needs a $kind.");

    internal bool Supported(JsonElement value, int depth = 0)
    {
        Check(depth);
        var kind = Kind(value);
        return kind switch
        {
            "comparison" or "logical" => table.Rows.ContainsKey(OperatorTable.Text(value, "operator")) && Supported(value.GetProperty("left"), depth + 1) && Supported(value.GetProperty("right"), depth + 1),
            "value" => new[] { "text", "number", "boolean", "null", "array", "composite" }.Contains(Kind(value.GetProperty("literal")), StringComparer.Ordinal),
            "resolved" or "eventContext" or "refusal" or "readPath" => true,
            _ => false
        };
    }

    internal void Write(JsonElement value, Action<string> text, Action<JsonElement> leaf, int depth = 0, int parent = 0, bool right = false)
    {
        Check(depth);
        if (new[] { "comparison", "logical" }.Contains(Kind(value), StringComparer.Ordinal))
        {
            var row = table.Rows[OperatorTable.Text(value, "operator")];
            var group = row.Precedence < parent || (right && row.Precedence == parent);
            if (group) text(table.Open);
            Write(value.GetProperty("left"), text, leaf, depth + 1, row.Precedence);
            text(row.Text);
            Write(value.GetProperty("right"), text, leaf, depth + 1, row.Precedence, true);
            if (group) text(table.Close);
        }
        else if (Kind(value) == "value" && !new[] { "array", "composite" }.Contains(Kind(value.GetProperty("literal")), StringComparer.Ordinal))
        {
            text(Literal(value.GetProperty("literal")));
        }
        else
        {
            leaf(value);
        }
    }

    void Check(int depth)
    {
        meter.Step();
        if (depth >= recursion) throw PlanMeter.Exceeded(TemplateCodes.Recursion, "Expression recursion limit exceeded.");
    }

    string Literal(JsonElement literal)
    {
        var kind = Kind(literal);
        if (kind == "null") return language.Null();
        var value = literal.GetProperty("value");
        return kind switch
        {
            "text" => language.String(value.GetString()!),
            "number" when value.GetDecimal() is var number && decimal.Truncate(number) == number && number >= long.MinValue && number <= long.MaxValue => language.Integer((long)number),
            "number" => language.Decimal(value.GetDecimal()),
            "boolean" => language.Boolean(value.GetBoolean()),
            _ => throw JsonScriptValues.Invalid("Unknown expression literal.")
        };
    }
}
