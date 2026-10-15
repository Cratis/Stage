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
    [InlineData(ValidationRuleKind.Checksum)]
    [InlineData((ValidationRuleKind)999)]
    void should_fail_closed_instead_of_emitting_a_todo(ValidationRuleKind kind)
    {
        var concept = _emailAddress with
        {
            Validations = [new DeclarativeValidateSyntax([new(ValidationRuleSyntax.ConceptValue, kind, new LiteralExpressionSyntax("luhn", SourceLocation.Start), null, SourceLocation.Start)], SourceLocation.Start)]
        };
        var error = Assert.Throws<UnsupportedValidationRule>(() => ConceptRenderer.Render(concept, _applicationSet, "Shop"));
        Assert.StartsWith("STAGE-ESM-042:", error.Message, StringComparison.Ordinal);
    }
}
