// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rules;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the declarative validation rules of a Screenplay command into Stage's per-property
/// <see cref="CommandPropertyRules"/>. Only <see cref="DeclarativeValidateSyntax"/> blocks are translated; inline
/// <c>validate csharp</c> blocks have no Stage rule equivalent and are skipped.
/// </summary>
public static class ValidationRuleConverter
{
    /// <summary>
    /// Converts the command's validation blocks into per-property rule groups, preserving declaration order.
    /// </summary>
    /// <param name="validations">The validation blocks declared on the command.</param>
    /// <returns>The rules grouped by property.</returns>
    public static IReadOnlyList<CommandPropertyRules> Convert(IEnumerable<ValidateSyntax> validations)
    {
        var groups = new List<(string Property, List<RuleDefinition> Rules)>();

        var rules = validations
            .OfType<DeclarativeValidateSyntax>()
            .SelectMany(block => block.Rules);

        foreach (var rule in rules)
        {
            var converted = Convert(rule);
            if (converted is null)
            {
                continue;
            }

            var group = groups.Find(candidate => string.Equals(candidate.Property, rule.Property, StringComparison.Ordinal));
            if (group.Rules is null)
            {
                group = (rule.Property, []);
                groups.Add(group);
            }

            group.Rules.Add(converted);
        }

        return [.. groups.Select(group => new CommandPropertyRules(group.Property, group.Rules))];
    }

    static RuleDefinition? Convert(ValidationRuleSyntax rule) =>
        rule.Rule switch
        {
            ValidationRuleKind.NotEmpty => new NotEmpty(rule.Message),
            ValidationRuleKind.Max => new MaxLength(IntOperand(rule), rule.Message),
            ValidationRuleKind.Min => new MinLength(IntOperand(rule), rule.Message),
            ValidationRuleKind.Length => new Length(IntOperand(rule), IntOperand(rule), rule.Message),
            ValidationRuleKind.Matches => new Matches(StringOperand(rule), rule.Message),
            ValidationRuleKind.GreaterThan => new GreaterThan(DoubleOperand(rule), rule.Message),
            ValidationRuleKind.GreaterThanOrEqual => new GreaterThanOrEqual(DoubleOperand(rule), rule.Message),
            ValidationRuleKind.LessThan => new LessThan(DoubleOperand(rule), rule.Message),
            ValidationRuleKind.LessThanOrEqual => new LessThanOrEqual(DoubleOperand(rule), rule.Message),

            // "all >" / "all >=" over a collection have no dedicated Stage rule; approximate with the scalar comparison.
            ValidationRuleKind.AllGreaterThan => new GreaterThan(DoubleOperand(rule), rule.Message),
            ValidationRuleKind.AllGreaterThanOrEqual => new GreaterThanOrEqual(DoubleOperand(rule), rule.Message),

            // Equality has no Stage rule vocabulary equivalent — skip it.
            _ => null
        };

    static int IntOperand(ValidationRuleSyntax rule) =>
        rule.Value is LiteralExpressionSyntax { Value: { } value } && double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? (int)number
            : 0;

    static double DoubleOperand(ValidationRuleSyntax rule) =>
        rule.Value is LiteralExpressionSyntax { Value: { } value } && double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0;

    static string StringOperand(ValidationRuleSyntax rule) =>
        rule.Value is LiteralExpressionSyntax { Value: { } value } ? System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
}
