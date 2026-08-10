// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Validation;

/// <summary>
/// Renders the FluentValidation call fragment for a Screenplay <see cref="ValidationRuleKind"/> — shared by
/// <c>ConceptValidator&lt;T&gt;</c> rendering (<c>ConceptRenderer</c>) and <c>CommandValidator&lt;T&gt;</c>
/// rendering (<c>StateChangeSliceRenderer</c>), which differ only in the subject the rule is applied to and how a
/// <see cref="ValidationRuleKind.Rule"/> custom predicate is wired up.
/// </summary>
public static class ValidationRuleRenderer
{
    /// <summary>
    /// Renders the FluentValidation call fragment (e.g. <c>.NotEmpty()</c>) for every rule kind except
    /// <see cref="ValidationRuleKind.Rule"/>, which has no fixed mapping — the caller renders that one itself
    /// since it requires synthesizing and registering a custom predicate method.
    /// </summary>
    /// <param name="kind">The rule kind.</param>
    /// <param name="value">The rendered comparison value, when the rule kind needs one.</param>
    /// <param name="subjectIsText">
    /// Whether the value being validated is text. Screenplay's <c>max</c>/<c>min</c> bound a number's magnitude but
    /// a string's <b>length</b>, and FluentValidation has a separate call for each — comparing a string against a
    /// number does not compile.
    /// </param>
    /// <returns>The call fragment, or <see langword="null"/> when the rule kind is not recognized here.</returns>
    public static string? RenderCall(ValidationRuleKind kind, string value, bool subjectIsText = false) => kind switch
    {
        ValidationRuleKind.NotEmpty => ".NotEmpty()",
        ValidationRuleKind.Max => subjectIsText ? $".MaximumLength({value})" : $".LessThanOrEqualTo({value})",
        ValidationRuleKind.Min => subjectIsText ? $".MinimumLength({value})" : $".GreaterThanOrEqualTo({value})",
        ValidationRuleKind.GreaterThan => $".GreaterThan({value})",
        ValidationRuleKind.GreaterThanOrEqual => $".GreaterThanOrEqualTo({value})",
        ValidationRuleKind.LessThan => $".LessThan({value})",
        ValidationRuleKind.LessThanOrEqual => $".LessThanOrEqualTo({value})",
        ValidationRuleKind.Equal => $".Equal({value})",
        ValidationRuleKind.Length => $".Length({value})",
        ValidationRuleKind.Matches => $".Matches({value})",
        _ => null,
    };

    /// <summary>
    /// Renders the <c>.WithMessage(...)</c> suffix for a rule, when it declares one.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The <c>.WithMessage(...)</c> fragment, or an empty string when the rule has no message.</returns>
    public static string RenderMessage(ValidationRuleSyntax rule) =>
        rule.Message is null ? string.Empty : $".WithMessage(\"{rule.Message.Replace("\"", "\\\"", StringComparison.Ordinal)}\")";
}
