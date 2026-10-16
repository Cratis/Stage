// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticMatchAnchors;

public class when_translating_character_sets : Specification
{
    [Theory]
    [InlineData(@"^\d$")]
    [InlineData(@"^\D$")]
    [InlineData(@"^\w$")]
    [InlineData(@"^\W$")]
    [InlineData(@"^\s$")]
    [InlineData(@"^\S$")]
    [InlineData("^.$")]
    [InlineData(@"^[\w\W]$")]
    [InlineData(@"^[^\w\W]$")]
    [InlineData(@"^[^\D0]$")]
    public void should_match_every_utf16_code_unit_like_the_reference(string pattern)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
        var regex = new Regex(SemanticMatchAnchors.Portable(pattern), RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
        for (var code = 0; code <= char.MaxValue; code++)
        {
            var input = ((char)code).ToString();
            var expected = SemanticValidationRules.Check(rule, SemanticValue.Text(input)).Status == SemanticRuleStatus.Satisfied;
            Assert.True(regex.IsMatch(input) == expected, $"{pattern}: U+{code:X4}");
        }
    }
}
