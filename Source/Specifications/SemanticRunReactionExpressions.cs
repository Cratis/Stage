// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

internal static class SemanticRunReactionExpressions
{
    static readonly Dictionary<SemanticComparisonOperator, Func<decimal, decimal, bool>> _orderings = new()
    {
        [SemanticComparisonOperator.GreaterThan] = (left, right) => left > right,
        [SemanticComparisonOperator.GreaterThanOrEqual] = (left, right) => left >= right,
        [SemanticComparisonOperator.LessThan] = (left, right) => left < right,
        [SemanticComparisonOperator.LessThanOrEqual] = (left, right) => left <= right
    };

    internal static SemanticValue Evaluate(SemanticExpression expression, IReadOnlyDictionary<SemanticId, SemanticValue> values, SemanticInvocationRefusalOutcome? refusal = null, DateTimeOffset? occurred = null) => expression switch
    {
        SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } when occurred is { } instant => SemanticValue.Text(instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
        SemanticRefusalExpression member when refusal is not null => refusal.ValueOf(member.Member),
        SemanticValueExpression literal => literal.Value,
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event or SemanticExpressionRootKind.Trigger, Source: SemanticExpressionSourceKind.Property } property when values.TryGetValue(property.Target, out var value) => value,
        _ => throw new InvalidSemanticContract("An execution expression is unresolved in its declared root scope.")
    };

    internal static bool Matches(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Matches(logical.Left, values) && Matches(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Matches(logical.Left, values) || Matches(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw new InvalidSemanticContract("Unknown condition node or operator.")
    };

    internal static void Validate(SemanticExecutionPlan plan, SemanticValue value, SemanticTypeReference type, string description)
    {
        if (value is SemanticNullValue)
        {
            if (!type.IsOptional) throw new InvalidSemanticContract($"A null {description} is incompatible with a required target.");
            return;
        }
        if (type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType) throw new UnsupportedSemanticMapping();
        var concept = ConceptOf(plan, type);
        if (!IsCompatible(value, concept?.Primitive ?? type.Primitive)) throw new InvalidSemanticContract($"A {description} is incompatible with its target type.");
        if (!IsDeclaredValue(concept, value)) throw new InvalidSemanticContract($"A {description} targets an enumeration concept but is not a declared value.");
    }

    static SemanticConcept? ConceptOf(SemanticExecutionPlan plan, SemanticTypeReference type) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target) : null;

    static bool IsCompatible(SemanticValue value, SemanticPrimitiveType primitive) => value switch
    {
        SemanticTextValue text => TextMatches(text.Value, primitive),
        SemanticNumberValue number => primitive == SemanticPrimitiveType.DecimalNumber || (primitive == SemanticPrimitiveType.WholeNumber && decimal.Truncate(number.Value) == number.Value),
        SemanticBooleanValue => primitive == SemanticPrimitiveType.Boolean,
        _ => false
    };

    static bool IsDeclaredValue(SemanticConcept? concept, SemanticValue value) =>
        concept is not { Values.IsEmpty: false } || (value is SemanticTextValue enumerated && concept.Values.Contains(enumerated.Value, StringComparer.Ordinal));

    static bool TextMatches(string value, SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Text => true,
        SemanticPrimitiveType.Uuid => Guid.TryParseExact(value, "D", out var uuid) && uuid.ToString("D", CultureInfo.InvariantCulture) == value,
        SemanticPrimitiveType.Date => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        SemanticPrimitiveType.DateTime => DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
        _ => false
    };

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var left = Operand(comparison.Left, values);
        var right = Operand(comparison.Right, values);

        return comparison.Operator switch
        {
            SemanticComparisonOperator.Equal => SemanticExpectationComparer.AreEqual(left, right),
            SemanticComparisonOperator.NotEqual => !SemanticExpectationComparer.AreEqual(left, right),
            _ => Ordered(comparison.Operator, left, right)
        };
    }

    static bool Ordered(SemanticComparisonOperator comparison, SemanticValue left, SemanticValue right)
    {
        if (left is not SemanticNumberValue a || right is not SemanticNumberValue b) throw new InvalidSemanticContract("Ordering condition requires numeric operands.");

        return _orderings.TryGetValue(comparison, out var ordered) ? ordered(a.Value, b.Value) : throw new InvalidSemanticContract("Unknown comparison operator.");
    }

    static SemanticValue Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values) => operand.Property.IsSet ? values[operand.Property] : operand.Value!;
}
