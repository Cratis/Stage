// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_checksum_rules
{
    [Theory]
    [InlineData("luhn", "79927398713", true)]
    [InlineData("luhn", "79927398714", false)]
    [InlineData("luhn", "", false)]
    [InlineData("luhn", "0", true)]
    [InlineData("luhn", "abc0", true)]
    [InlineData("luhn", " 0", true)]
    [InlineData("luhn", "0 ", false)]
    [InlineData("luhn", "\t0", false)]
    [InlineData("luhn", "é0", false)]
    [InlineData("mod11", "123456785", true)]
    [InlineData("mod11", "123456784", false)]
    [InlineData("mod11", "60", false)]
    [InlineData("mod11", "", false)]
    [InlineData("mod11", "0", true)]
    [InlineData("mod11", "abc0", true)]
    [InlineData("mod97", "12345678978", true)]
    [InlineData("mod97", "12345678979", false)]
    [InlineData("mod97", "", false)]
    [InlineData("mod97", "8", false)]
    [InlineData("mod97", "98", true)]
    [InlineData("mod97", "abc98", true)]
    [InlineData("mod97", "ABC98", true)]
    [InlineData("mod97", " 98", true)]
    [InlineData("mod97", "98 ", false)]
    [InlineData("mod97", "\n98", false)]
    public void should_match_screenplay_check_digits(string algorithm, string value, bool expected)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text(algorithm), null);
        SemanticRuleValues.Satisfies(rule, SemanticValue.Text(value), () => new UnsupportedSemanticMapping()).ShouldEqual(expected);
    }
}
