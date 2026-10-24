// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_refusing_unsupported_validation : given.concepts
{
    [Theory]
    [InlineData(ValidationRuleKind.Checksum, false)]
    [InlineData((ValidationRuleKind)999, false)]
    [InlineData((ValidationRuleKind)999, true)]
    void should_fail_closed_instead_of_emitting_a_todo(ValidationRuleKind kind, bool nonLiteral)
    {
        var concept = _emailAddress with
        {
            Validations = [new DeclarativeValidateSyntax([new(ValidationRuleSyntax.ConceptValue, kind, nonLiteral ? new PathExpressionSyntax("missing", SourceLocation.Start) : new LiteralExpressionSyntax("luhn", SourceLocation.Start), null, SourceLocation.Start)], SourceLocation.Start)]
        };
        var error = Assert.Throws<UnsupportedValidationRule>(() => ConceptRenderer.Render(concept, _applicationSet, "Shop"));
        Assert.StartsWith(kind == (ValidationRuleKind)999 ? "STAGE-ESM-005:" : "STAGE-ESM-042:", error.Message, StringComparison.Ordinal);
        Assert.Contains(kind.ToString(), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    void should_preserve_the_todo_for_a_match_against_an_out_of_scope_value()
    {
        var concept = _emailAddress with
        {
            Validations = [new DeclarativeValidateSyntax([new(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Matches, new PathExpressionSyntax("missing", SourceLocation.Start), null, SourceLocation.Start)], SourceLocation.Start)]
        };
        var file = ConceptRenderer.Render(concept, _applicationSet, "Shop");
        Assert.Contains($"// TODO: validation rule 'Matches' on '{ValidationRuleSyntax.ConceptValue}' compares against a value outside the concept's scope", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("PortableMatchValidator", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    void should_refuse_a_match_without_a_pattern_with_an_explicit_diagnostic()
    {
        var concept = _emailAddress with
        {
            Validations = [new DeclarativeValidateSyntax([new(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Matches, null, null, SourceLocation.Start)], SourceLocation.Start)]
        };
        var error = Assert.Throws<UnsupportedValidationRule>(() => ConceptRenderer.Render(concept, _applicationSet, "Shop"));
        Assert.Contains("matches requires a portable constant text pattern", error.Message, StringComparison.Ordinal);
    }
}
