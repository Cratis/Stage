// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Parses and evaluates the template expression a capture append can be guarded by:
/// <c lang="csharp">status == "sent" &amp;&amp; overdue == true</c>.
/// </summary>
/// <remarks>
/// The grammar is deliberately small, so every target evaluates it the same way: fields, string, number, boolean and
/// null literals, equality and ordering comparisons, conjunction, disjunction, negation and parentheses.
/// A field standing alone is true when it holds the boolean <see langword="true"/>. Ordering compares numbers only.
/// </remarks>
internal static partial class SemanticRunCaptureExpression
{
    /// <summary>
    /// Parses an expression, with or without its enclosing backticks.
    /// </summary>
    /// <param name="text">The expression text.</param>
    /// <param name="expression">The parsed expression.</param>
    /// <returns><see langword="true"/> when the text is an expression of the grammar.</returns>
    public static bool TryParse(string? text, out Node expression)
    {
        expression = new Literal(SemanticValue.Null);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var body = text.Trim();
        if (body.Length >= 2 && body[0] == '`' && body[^1] == '`')
        {
            body = body[1..^1];
        }

        var parser = new Parser(body);
        if (!parser.TryParse(out var parsed))
        {
            return false;
        }

        expression = parsed;
        return true;
    }

    /// <summary>
    /// Evaluates a parsed expression over an item's fields.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="field">Looks up a field value; <see langword="null"/> when the item has no such field.</param>
    /// <returns>Whether the expression holds.</returns>
    /// <exception cref="InvalidSemanticContract">The expression orders values that are not numbers.</exception>
    public static bool Evaluate(Node expression, Func<string, SemanticValue?> field) =>
        Evaluate(expression, name => new Lookup(field(name), null)).Holds;

    /// <summary>
    /// Evaluates an expression without treating an unsupported lookup as a missing field.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="field">Looks up a scalar, a missing field, or an explicitly unsupported source.</param>
    /// <returns>The boolean outcome or the first reached unsupported lookup.</returns>
    /// <exception cref="InvalidSemanticContract">The expression contains an unknown node or orders values that are not numbers.</exception>
    public static Evaluation Evaluate(Node expression, Func<string, Lookup> field)
    {
        switch (expression)
        {
            case Or or:
                var leftOr = Evaluate(or.Left, field);
                return leftOr.Unsupported is not null || leftOr.Holds ? leftOr : Evaluate(or.Right, field);
            case And and:
                var leftAnd = Evaluate(and.Left, field);
                return leftAnd.Unsupported is not null || !leftAnd.Holds ? leftAnd : Evaluate(and.Right, field);
            case Not not:
                var operand = Evaluate(not.Operand, field);
                return operand.Unsupported is not null ? operand : new(!operand.Holds, null);
            case Comparison comparison:
                return Compare(comparison, field);
            case Field single:
                var value = field(single.Name);
                return new(value.Value is SemanticBooleanValue { Value: true }, value.Unsupported);
            case Literal literal:
                return new(literal.Value is SemanticBooleanValue { Value: true }, null);
            default:
                throw new InvalidSemanticContract("A capture expression node is unknown.");
        }
    }

    static Evaluation Compare(Comparison comparison, Func<string, Lookup> field)
    {
        var firstLookup = Operand(comparison.Left, field);
        if (firstLookup.Unsupported is not null) return new(false, firstLookup.Unsupported);
        var secondLookup = Operand(comparison.Right, field);
        if (secondLookup.Unsupported is not null) return new(false, secondLookup.Unsupported);
        var left = firstLookup.Value ?? SemanticValue.Null;
        var right = secondLookup.Value ?? SemanticValue.Null;
        if (string.Equals(comparison.Operator, "==", StringComparison.Ordinal) || string.Equals(comparison.Operator, "!=", StringComparison.Ordinal))
        {
            var equal = left is SemanticNumberValue leftNumber && right is SemanticNumberValue rightNumber
                ? leftNumber.Value == rightNumber.Value
                : SemanticExpectationComparer.AreEqual(left, right);
            return new(string.Equals(comparison.Operator, "==", StringComparison.Ordinal) ? equal : !equal, null);
        }

        if (left is not SemanticNumberValue first || right is not SemanticNumberValue second)
        {
            throw new InvalidSemanticContract($"The capture expression orders with '{comparison.Operator}' values that are not numbers.");
        }

        var holds = comparison.Operator switch
        {
            "<" => first.Value < second.Value,
            "<=" => first.Value <= second.Value,
            ">" => first.Value > second.Value,
            _ => first.Value >= second.Value
        };
        return new(holds, null);
    }

    static Lookup Operand(Node node, Func<string, Lookup> field) => node switch
    {
        Field named => field(named.Name),
        Literal literal => new(literal.Value, null),
        _ => throw new InvalidSemanticContract("A capture expression operand must be a field or a literal.")
    };

    /// <summary>
    /// A scalar or missing value, or a reached source the evaluator cannot read.
    /// </summary>
    /// <param name="Value">The scalar, or null for a missing field.</param>
    /// <param name="Unsupported">The unsupported source, never a missing value.</param>
    internal readonly record struct Lookup(SemanticValue? Value, string? Unsupported);

    /// <summary>
    /// A supported boolean outcome or a reached unsupported lookup.
    /// </summary>
    /// <param name="Holds">Whether a supported expression holds.</param>
    /// <param name="Unsupported">The reached unsupported source.</param>
    internal readonly record struct Evaluation(bool Holds, string? Unsupported);

    /// <summary>
    /// A node of a parsed capture expression.
    /// </summary>
    internal abstract record Node;

    /// <summary>
    /// Either side holds.
    /// </summary>
    /// <param name="Left">The left side.</param>
    /// <param name="Right">The right side.</param>
    internal sealed record Or(Node Left, Node Right) : Node;

    /// <summary>
    /// Both sides hold.
    /// </summary>
    /// <param name="Left">The left side.</param>
    /// <param name="Right">The right side.</param>
    internal sealed record And(Node Left, Node Right) : Node;

    /// <summary>
    /// The operand does not hold.
    /// </summary>
    /// <param name="Operand">The negated expression.</param>
    internal sealed record Not(Node Operand) : Node;

    /// <summary>
    /// A comparison of two operands.
    /// </summary>
    /// <param name="Left">The left operand.</param>
    /// <param name="Operator">The comparison operator.</param>
    /// <param name="Right">The right operand.</param>
    internal sealed record Comparison(Node Left, string Operator, Node Right) : Node;

    /// <summary>
    /// The value of a field of the item.
    /// </summary>
    /// <param name="Name">The field name.</param>
    internal sealed record Field(string Name) : Node;

    /// <summary>
    /// A literal value.
    /// </summary>
    /// <param name="Value">The value.</param>
    internal sealed record Literal(SemanticValue Value) : Node;
}
