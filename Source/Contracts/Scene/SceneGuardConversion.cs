// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Guards;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

// Retains authored details that Scene's closed vocabulary cannot represent, for the legacy projection.
internal abstract record SceneGuardConversion
{
    internal abstract GuardCondition ToCondition();

    internal abstract IDictionary<string, object?> ToLegacy();

    internal static SceneGuardConversion From(ScreenplaySyntax.ConditionSyntax? condition, Func<string, IReadOnlyList<string>?>? enumerationOf, Func<string, bool>? isStringField = null) => condition switch
    {
        ScreenplaySyntax.ComparisonConditionSyntax comparison => new Comparison(
            comparison.Left,
            comparison.Operator.ToString(),
            comparison.Right is ScreenplaySyntax.LiteralExpressionSyntax literal ? literal.Value : null,
            comparison.Right is ScreenplaySyntax.LiteralExpressionSyntax ? null : comparison.Right.GetType().Name,
            enumerationOf?.Invoke(comparison.Left),
            isStringField?.Invoke(comparison.Left) == true),
        ScreenplaySyntax.LogicalConditionSyntax logical => new Logical(
            From(logical.Left, enumerationOf, isStringField), logical.Operator.ToString(), From(logical.Right, enumerationOf, isStringField)),
        null => new Unsupported(null),
        _ => new Unsupported(condition.GetType())
    };

    internal static IDictionary<string, object?> LegacySubject(SubjectReference subject) => new Dictionary<string, object?> { ["subject"] = subject.Subject };

    sealed record Comparison(string Path, string Operation, object? Value, string? UnsupportedExpression, IReadOnlyList<string>? Enumeration, bool IsStringField) : SceneGuardConversion
    {
        internal override GuardCondition ToCondition() => new ComparisonGuardCondition(
            SceneGuards.SubjectReferenceFor(Path),
            Enum.TryParse<GuardOperator>(Operation, out var operation) && (operation != GuardOperator.Contains || (IsStringField && Value is string))
                ? operation : (GuardOperator)(-1),
            UnsupportedExpression is { } kind ? new { unsupported = kind } : Value,
            Enumeration);

        internal override IDictionary<string, object?> ToLegacy()
        {
            var result = new Dictionary<string, object?>
            {
                ["kind"] = "comparison",
                ["left"] = LegacySubject(SceneGuards.SubjectReferenceFor(Path)),
                ["operator"] = Operation,
                ["right"] = UnsupportedExpression is { } kind ? new Dictionary<string, object?> { ["kind"] = kind } : Value,
            };
            if (Enumeration is not null) result["enumeration"] = Enumeration;
            return result;
        }
    }

    sealed record Logical(SceneGuardConversion Left, string Operation, SceneGuardConversion Right) : SceneGuardConversion
    {
        internal override GuardCondition ToCondition() => new LogicalGuardCondition(
            Left.ToCondition(),
            Enum.TryParse<LogicalGuardOperator>(Operation, out var operation) ? operation : (LogicalGuardOperator)(-1),
            Right.ToCondition());

        internal override IDictionary<string, object?> ToLegacy() => new Dictionary<string, object?>
        {
            ["kind"] = "logical",
            ["left"] = Left.ToLegacy(),
            ["operator"] = Operation,
            ["right"] = Right.ToLegacy(),
        };
    }

    sealed record Unsupported(Type? SyntaxType) : SceneGuardConversion
    {
        internal override GuardCondition ToCondition() => new ComparisonGuardCondition(SceneGuards.SubjectReferenceFor(string.Empty), GuardOperator.Equal, null);

        internal override IDictionary<string, object?> ToLegacy() => new Dictionary<string, object?> { ["kind"] = SyntaxType!.Name };
    }
}
