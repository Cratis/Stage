// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rules;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the declarative validation rules of a Screenplay command into Stage's per-property
/// <see cref="CommandPropertyRules"/>. Only <see cref="DeclarativeValidateSyntax"/> blocks are translated; inline
/// <c language="csharp">validate csharp</c> blocks have no Stage rule equivalent and are skipped, as is any rule whose operand is not a
/// literal — a Stage rule holds a fixed value, and there is none to hold for one stated against a runtime value.
/// Collection quantifiers, equality, checksums and named rules have no Stage rule equivalent and are skipped.
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

    // A Stage rule holds a fixed operand, and Screenplay lets one be stated against a value the application only
    // resolves while it runs — 'dueDate > today', or a threshold naming another property. There is no fixed value to
    // store for those, so the rule is dropped rather than stored with a stand-in: a rule carrying a substituted
    // operand asserts something the document never said, which is worse than carrying no rule at all.
    static RuleDefinition? Convert(ValidationRuleSyntax rule) =>
        rule.Rule switch
        {
            ValidationRuleKind.NotEmpty => new NotEmpty(rule.Message),

            // Bounds measure numeric values or text length at runtime; length-only rules lose numeric semantics.
            ValidationRuleKind.Max when DoubleOperand(rule) is { } max => new LessThanOrEqual(max, rule.Message),
            ValidationRuleKind.Min when DoubleOperand(rule) is { } min => new GreaterThanOrEqual(min, rule.Message),
            ValidationRuleKind.Length when IntOperand(rule) is { } length => new Length(length, length, rule.Message),
            ValidationRuleKind.Matches when StringOperand(rule) is { } pattern => new Matches(pattern, rule.Message),
            ValidationRuleKind.GreaterThan when DoubleOperand(rule) is { } threshold => new GreaterThan(threshold, rule.Message),
            ValidationRuleKind.GreaterThanOrEqual when DoubleOperand(rule) is { } threshold => new GreaterThanOrEqual(threshold, rule.Message),
            ValidationRuleKind.LessThan when DoubleOperand(rule) is { } threshold => new LessThan(threshold, rule.Message),
            ValidationRuleKind.LessThanOrEqual when DoubleOperand(rule) is { } threshold => new LessThanOrEqual(threshold, rule.Message),

            // A scalar comparison cannot represent "all", even when the element path is known.
            // Drop quantifiers, equality, checksums and named rules rather than partially enforcing them.
            _ => null
        };

    static int? IntOperand(ValidationRuleSyntax rule) =>
        DoubleOperand(rule) is { } number && number is >= 0 and <= int.MaxValue && Math.Truncate(number) == number ? (int)number : null;

    static double? DoubleOperand(ValidationRuleSyntax rule) =>
        StringOperand(rule) is { } text && double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    static string? StringOperand(ValidationRuleSyntax rule) =>
        rule.Value is LiteralExpressionSyntax { Value: { } value }
            ? System.Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
}
