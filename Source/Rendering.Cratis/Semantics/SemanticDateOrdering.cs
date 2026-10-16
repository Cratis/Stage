// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

// Mirrors Screenplay 4.199.0 SemanticDateComparison and SemanticCommandDateComparisons (decision 0087).
// Replace with the public API requested in https://github.com/Cratis/Screenplay/issues/842 when available.
// Chronological equality applies only to command comparisons; specification expectations stay ordinal.
internal static class SemanticDateOrdering
{
    internal static bool IsDate(SemanticPrimitiveType primitive) => primitive is SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

    internal static bool IsComparison(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or
        SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
        SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual;

    internal static SemanticPrimitiveType Primitive(IEnumerable<SemanticConcept> concepts, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept => concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive ?? SemanticPrimitiveType.Unknown,
        _ => SemanticPrimitiveType.Unknown
    };

    internal static IEnumerable<SemanticProperty> Properties(IEnumerable<SemanticConcept> concepts, SemanticCommand command)
    {
        var conditions = command.Requirements.Select(requirement => requirement.Condition)
            .Concat(command.Produces.Select(production => production.When).OfType<SemanticCondition>());
        var ids = conditions.SelectMany(Operands).Where(operand => operand.Read is null && operand.Property.IsSet).Select(operand => operand.Property)
            .Concat(command.Validations.Where(rule => IsComparison(rule.Kind)).Select(rule => rule.Property)).ToHashSet();

        return command.Properties.Where(property => ids.Contains(property.Id) && IsDate(Primitive(concepts, property.Type)));
    }

    internal static IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> Participating(IEnumerable<SemanticConcept> concepts, SemanticCommand command) =>
        Properties(concepts, command).ToDictionary(property => property.Id, property => Primitive(concepts, property.Type));

    internal static string? InputFailure(SemanticApplication application, SemanticCommand command, IEnumerable<SemanticPropertyValue> values)
    {
        var inputs = values.ToArray();
        foreach (var property in Properties(application.Concepts, command))
        {
            if (Primitive(application.Concepts, property.Type) == SemanticPrimitiveType.DateTime &&
                inputs.FirstOrDefault(value => value?.TargetProperty == property.Id)?.Value is SemanticTextValue text && !TryInstant(text.Value, out _))
            {
                return $"DateTime command property '{property.Name}' requires an explicit offset or Z.";
            }
        }

        return null;
    }

    internal static bool TryInstant(string value, out DateTimeOffset instant) => DateTimeOffset.TryParseExact(
        value,
        ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm'Z'"],
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal,
        out instant);

    internal static int Compare(SemanticValue left, SemanticValue right, SemanticPrimitiveType primitive)
    {
        if (left is not SemanticTextValue first || right is not SemanticTextValue second)
            throw new InvalidSemanticContract("A date comparison requires text values of its declared date primitive.");
        if (primitive == SemanticPrimitiveType.Date &&
            DateOnly.TryParseExact(first.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDate) &&
            DateOnly.TryParseExact(second.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var secondDate))
        {
            return firstDate.CompareTo(secondDate);
        }
        if (primitive == SemanticPrimitiveType.DateTime && TryInstant(first.Value, out var firstInstant) && TryInstant(second.Value, out var secondInstant))
            return firstInstant.UtcTicks.CompareTo(secondInstant.UtcTicks);

        throw new InvalidSemanticContract("A date comparison requires valid dates or explicitly offset instants.");
    }

    internal static bool Holds(int comparison, SemanticComparisonOperator operation) => operation switch
    {
        SemanticComparisonOperator.Equal => comparison == 0,
        SemanticComparisonOperator.NotEqual => comparison != 0,
        SemanticComparisonOperator.GreaterThan => comparison > 0,
        SemanticComparisonOperator.GreaterThanOrEqual => comparison >= 0,
        SemanticComparisonOperator.LessThan => comparison < 0,
        SemanticComparisonOperator.LessThanOrEqual => comparison <= 0,
        _ => throw new InvalidSemanticContract("Unknown comparison operator.")
    };

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value, SemanticPrimitiveType primitive)
    {
        if (value is SemanticNullValue) return true;
        var comparison = Compare(value, rule.Operand!, primitive);

        return rule.Kind switch
        {
            SemanticValidationRuleKind.Equal => comparison == 0,
            SemanticValidationRuleKind.NotEqual => comparison != 0,
            SemanticValidationRuleKind.GreaterThan => comparison > 0,
            SemanticValidationRuleKind.GreaterThanOrEqual => comparison >= 0,
            SemanticValidationRuleKind.LessThan => comparison < 0,
            SemanticValidationRuleKind.LessThanOrEqual => comparison <= 0,
            _ => throw new InvalidSemanticContract("Unknown date comparison rule.")
        };
    }

    internal static string DefaultMessage(SemanticValidationRule rule) => rule.Kind switch
    {
        SemanticValidationRuleKind.Equal => $"A value must equal '{((SemanticTextValue)rule.Operand!).Value}'.",
        SemanticValidationRuleKind.NotEqual => $"A value must not equal '{((SemanticTextValue)rule.Operand!).Value}'.",
        SemanticValidationRuleKind.GreaterThan => $"A value must be greater than '{((SemanticTextValue)rule.Operand!).Value}'.",
        SemanticValidationRuleKind.GreaterThanOrEqual => $"A value must be at least '{((SemanticTextValue)rule.Operand!).Value}'.",
        SemanticValidationRuleKind.LessThan => $"A value must be less than '{((SemanticTextValue)rule.Operand!).Value}'.",
        SemanticValidationRuleKind.LessThanOrEqual => $"A value must be at most '{((SemanticTextValue)rule.Operand!).Value}'.",
        _ => throw new InvalidSemanticContract("Unknown date comparison rule.")
    };

    static IEnumerable<SemanticConditionOperand> Operands(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => [comparison.Left, comparison.Right],
        SemanticLogicalCondition logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };
}
