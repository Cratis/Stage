// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class ExpressionLowerer(OperatorTable operators, ITemplateLanguage language, PlanMeter meter, int recursion)
{
    internal SegmentHandle? Lower(object? value)
    {
        if (value is not JsonScriptValues.JsonObject expression) throw JsonScriptValues.Invalid("Expected an expression JSON object.");
        var lowered = Visit(expression.Source, 0);
        return lowered is null ? null : new(lowered.Segments);
    }

    static ImmutableArray<TemplateSegment> Child(Node child, OperatorTable.Row parent, bool right)
    {
        var group = child.Precedence < parent.Precedence || (child.Precedence == parent.Precedence &&
            (parent.Position == "unary" || parent.Associativity == "none" || child.Associativity != parent.Associativity || (right ? parent.Associativity == "left" : parent.Associativity == "right")));
        return group ? [new TemplateTextSegment("("), .. child.Segments, new TemplateTextSegment(")")] : child.Segments;
    }

    Node? Visit(JsonElement value, int depth)
    {
        meter.Step();
        if (depth >= recursion) throw PlanMeter.Exceeded(TemplateCodes.Recursion, "Expression recursion limit exceeded.");
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var kind)) throw JsonScriptValues.Invalid("Expression needs a kind.");
        switch (kind.GetString())
        {
            case "literal":
                OperatorTable.Fields(value, ["kind", "value"]);
                return new([new TemplateTextSegment(Literal(value.GetProperty("value")))], 1001, "none");
            case "symbol":
                OperatorTable.Fields(value, ["kind", "symbol"]);
                return new([new TemplateSymbolUseSegment(SymbolHandle.Parse(JsonScriptValues.Convert(value.GetProperty("symbol"), 256 * 1024)))], 1001, "none");
            case "unary":
                OperatorTable.Fields(value, ["kind", "operator", "operand"]);
                var operand = Visit(value.GetProperty("operand"), depth + 1);
                if (!operators.TryGet("unary", OperatorTable.Text(value, "operator"), out var unary) || operand is null) return null;
                return new([new TemplateTextSegment(unary.Token + " "), .. Child(operand, unary, true)], unary.Precedence, unary.Associativity);
            case "binary":
                OperatorTable.Fields(value, ["kind", "operator", "left", "right"]);
                var left = Visit(value.GetProperty("left"), depth + 1);
                var right = Visit(value.GetProperty("right"), depth + 1);
                if (!operators.TryGet("binary", OperatorTable.Text(value, "operator"), out var binary) || left is null || right is null) return null;
                return new([.. Child(left, binary, false), new TemplateTextSegment(" " + binary.Token + " "), .. Child(right, binary, true)], binary.Precedence, binary.Associativity);
            default:
                // Unknown well-shaped kinds are semantic unsupported nodes, not approximations.
                return null;
        }
    }

    string Literal(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => language.EscapeString(value.GetString()!),
        JsonValueKind.Number when value.TryGetInt64(out var integer) => language.FormatInteger(integer),
        JsonValueKind.Number when value.TryGetDecimal(out var number) => language.FormatDecimal(number),
        JsonValueKind.True => language.FormatBoolean(true),
        JsonValueKind.False => language.FormatBoolean(false),
        JsonValueKind.Null => language.FormatNull(),
        _ => throw JsonScriptValues.Invalid("Expression literal must be a primitive JSON value.")
    };

    sealed record Node(ImmutableArray<TemplateSegment> Segments, int Precedence, string Associativity);
}
