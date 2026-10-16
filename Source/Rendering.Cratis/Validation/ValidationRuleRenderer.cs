// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis.CSharp;

using CSharpLiteral = Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax;

namespace Cratis.Stage.Rendering.Cratis.Validation;

/// <summary>
/// Renders the FluentValidation call fragment for a Screenplay <see cref="ValidationRuleKind"/> — shared by
/// <c language="csharp">ConceptValidator&lt;T&gt;</c> rendering (<c language="csharp">ConceptRenderer</c>) and <c language="csharp">CommandValidator&lt;T&gt;</c>
/// rendering (<c language="csharp">StateChangeSliceRenderer</c>), which differ only in the subject the rule is applied to and how a
/// <see cref="ValidationRuleKind.Rule"/> custom predicate is wired up.
/// </summary>
public static class ValidationRuleRenderer
{
    /// <summary>
    /// Renders the FluentValidation call fragment (e.g. <c language="csharp">.NotEmpty()</c>) for every rule kind except
    /// <see cref="ValidationRuleKind.Rule"/>, which has no fixed mapping — the caller renders that one itself
    /// since it requires synthesizing and registering a custom predicate method.
    /// </summary>
    /// <param name="kind">The rule kind.</param>
    /// <param name="value">The rendered comparison value, when the rule kind needs one.</param>
    /// <param name="subjectIsText">
    /// Whether the value being validated is text. Screenplay's <c language="csharp">max</c>/<c language="csharp">min</c> bound a number's magnitude but
    /// a string's <b>length</b>, and FluentValidation has a separate call for each — comparing a string against a
    /// number does not compile.
    /// </param>
    /// <returns>The call fragment.</returns>
    /// <exception cref="UnsupportedValidationRule">Thrown when an unmapped rule kind escapes admission.</exception>
    public static string RenderCall(ValidationRuleKind kind, string value, bool subjectIsText = false) => kind switch
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
        ValidationRuleKind.Matches => RenderMatch(value),
        _ => throw new UnsupportedValidationRule(kind),
    };

    /// <summary>
    /// Renders the <c language="csharp">.WithMessage(...)</c> suffix for a rule, when it declares one.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The <c language="csharp">.WithMessage(...)</c> fragment, or an empty string when the rule has no message.</returns>
    public static string RenderMessage(ValidationRuleSyntax rule) =>
        rule.Message is null ? string.Empty : $".WithMessage({CSharpCodeBuilder.StringLiteral(rule.Message)})";

    internal static void EnsureSupported(ValidationRuleSyntax rule)
    {
        if (rule.Rule == ValidationRuleKind.Rule && rule.Code is not null)
        {
            return;
        }
        if (rule.Rule is ValidationRuleKind.NotEmpty or ValidationRuleKind.Max or ValidationRuleKind.Min or
            ValidationRuleKind.GreaterThan or ValidationRuleKind.GreaterThanOrEqual or ValidationRuleKind.LessThan or
            ValidationRuleKind.LessThanOrEqual or ValidationRuleKind.Equal or ValidationRuleKind.Length or ValidationRuleKind.Matches)
        {
            return;
        }

        throw new UnsupportedValidationRule(rule);
    }

    static string RenderMatch(string value)
    {
        if (SyntaxFactory.ParseExpression(value) is not CSharpLiteral literal || literal.RawKind != (int)SyntaxKind.StringLiteralExpression)
        {
            return $".Matches({value})";
        }

        var pattern = CSharpCodeBuilder.StringLiteral(SemanticMatchAnchors.Portable(literal.Token.ValueText));

        return $".Matches(new global::System.Text.RegularExpressions.Regex({pattern}, global::System.Text.RegularExpressions.RegexOptions.ECMAScript | global::System.Text.RegularExpressions.RegexOptions.CultureInvariant, global::System.TimeSpan.FromSeconds(1)))";
    }
}
