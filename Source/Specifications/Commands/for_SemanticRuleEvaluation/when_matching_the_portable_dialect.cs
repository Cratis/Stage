// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_matching_the_portable_dialect : Specification
{
    [Theory]
    [InlineData("^.$", "\r", false)]
    [InlineData("^.$", "\u2028", false)]
    [InlineData("^.$", "\u2029", false)]
    [InlineData(@"^\s$", "\u00a0", true)]
    [InlineData(@"^\s$", "\ufeff", true)]
    [InlineData(@"^\s$", "\u0085", false)]
    [InlineData(@"^\S$", "\u00a0", false)]
    [InlineData(@"^[\s]$", "\u2003", true)]
    [InlineData(@"^[^\s]$", "\u3000", false)]
    [InlineData(@"^\w$", "\u0130", false)]
    [InlineData(@"^\W$", "\u0130", true)]
    [InlineData(@"^[\w]$", "\u0130", false)]
    [InlineData(@"^\w$", "é", false)]
    [InlineData(@"^\d$", "\u0661", false)]
    [InlineData("ABC$", "ABC\n", false)]
    [InlineData("B", "ABC", true)]
    [InlineData("^abc$", "ABC", false)]
    [InlineData("^.*$", null, true)]
    [InlineData(@"^([^X][\s\S]*|X[\s\S]+)?$", "X\n", true)]
    [InlineData(@"^([^X][\s\S]*|X[\s\S]+)?$", "X", false)]
    [InlineData(@"^([^X][\s\S]*|X[\s\S]+)?$", "", true)]
    public void should_agree_with_the_public_reference_check(string pattern, string? input, bool expected)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
        var value = input is null ? SemanticValue.Null : SemanticValue.Text(input);
        var reference = SemanticValidationRules.Check(rule, value);
        (reference.Status == SemanticRuleStatus.Satisfied).ShouldEqual(expected);
        SemanticRuleValues.Satisfies(rule, value, () => new InvalidSemanticContract("Unexpected unsupported match.")).ShouldEqual(expected);
    }
}
