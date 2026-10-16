// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_refusing_unrepresentable_matches : Specification
{
    [Theory]
    [InlineData("(){999999999999999999999999999}", "QuantifierOrCaptureGroupOutOfRange")]
    [InlineData("(?=a)a", "lookaround")]
    [InlineData(@"\bword", "word boundary")]
    [InlineData(@"\p{L}", "Unicode property escape")]
    [InlineData("(?i)a", "inline options")]
    [InlineData("[z-a]", "reversed character range")]
    [InlineData("^[]$]+$", "empty character class")]
    [InlineData("^A[^]$", "negated empty character class")]
    public void should_name_the_construct_in_the_existing_typed_refusal(string pattern, string construct)
    {
        var error = Assert.Throws<UnsupportedValidationRule>(() => ValidationRuleRenderer.RenderCall(ValidationRuleKind.Matches, CSharpCodeBuilder.StringLiteral(pattern)));
        error.Message.ShouldContain("STAGE-ESM-005");
        error.Message.ShouldContain(construct);
    }
}
