// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRuleValues
{
    internal static string? GeneratedFailure(SemanticConcept concept, string property, SemanticValue value) =>
        concept.Validations.Any(rule => !Satisfies(rule, value, () => new InvalidSemanticContract("A generated concept rule escaped admission.")))
            ? SemanticGenerationShapes.RuleMessage(property, concept.Name) : null;

    internal static bool CanEvaluate(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or SemanticValidationRuleKind.GreaterThan or
        SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual or
        SemanticValidationRuleKind.Length or SemanticValidationRuleKind.Matches or SemanticValidationRuleKind.AllGreaterThan or
        SemanticValidationRuleKind.AllGreaterThanOrEqual or SemanticValidationRuleKind.Checksum or
        SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith;

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value, Func<Exception> unsupported) => rule.Kind switch
    {
        SemanticValidationRuleKind.NotEmpty => !Empty(value),
        SemanticValidationRuleKind.Matches => Matches(rule, value, unsupported),
        _ when value is SemanticNullValue => true,
        SemanticValidationRuleKind.Maximum => Measure(value, unsupported) <= Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.Minimum => Measure(value, unsupported) >= Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.Equal => AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.NotEqual => !AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.GreaterThan => Measure(value, unsupported) > Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.GreaterThanOrEqual => Measure(value, unsupported) >= Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.LessThan => Measure(value, unsupported) < Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.LessThanOrEqual => Measure(value, unsupported) <= Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.Length => value is SemanticTextValue text && text.Value.Length == Number(rule.Operand, unsupported),
        SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith =>
            SemanticValidationRules.Check(rule, value).Status == SemanticRuleStatus.Satisfied,
        SemanticValidationRuleKind.Checksum => SemanticChecksumRules.Satisfies(value, rule.Operand, unsupported),
        SemanticValidationRuleKind.AllGreaterThan => value is SemanticArrayValue array && array.Values.All(element => Measure(element, unsupported) > Number(rule.Operand, unsupported)),
        SemanticValidationRuleKind.AllGreaterThanOrEqual => value is SemanticArrayValue array && array.Values.All(element => Measure(element, unsupported) >= Number(rule.Operand, unsupported)),
        _ => throw unsupported()
    };

    internal static bool Empty(SemanticValue value) => value is SemanticNullValue || (value is SemanticTextValue text && string.IsNullOrWhiteSpace(text.Value)) || value is SemanticArrayValue { Values.IsEmpty: true };

    internal static bool AreEqual(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticNullValue, SemanticNullValue) => true,
        (SemanticTextValue a, SemanticTextValue b) => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
        (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
        (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
        (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => AreEqual(pair.First, pair.Second)),
        (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length &&
            a.Properties.All(property => b.Properties.Any(candidate => candidate.TargetProperty == property.TargetProperty && AreEqual(property.Value, candidate.Value))),
        _ => false
    };

    static bool Matches(SemanticValidationRule rule, SemanticValue value, Func<Exception> unsupported) =>
        SemanticValidationRules.Check(rule, value).Status switch
        {
            SemanticRuleStatus.Satisfied => true,
            SemanticRuleStatus.Failed => false,
            _ => throw unsupported()
        };

    static decimal Measure(SemanticValue value, Func<Exception> unsupported) => value switch
    {
        SemanticTextValue text => text.Value.Length,
        SemanticNumberValue number => number.Value,
        _ => throw unsupported()
    };

    static decimal Number(SemanticValue? value, Func<Exception> unsupported) => value is SemanticNumberValue number ? number.Value : throw unsupported();
}
