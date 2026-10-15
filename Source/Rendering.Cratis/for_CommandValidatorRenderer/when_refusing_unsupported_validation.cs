// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CommandValidatorRenderer;

public class when_refusing_unsupported_validation : Specification
{
    [Theory]
    [InlineData(ValidationRuleKind.Checksum)]
    [InlineData((ValidationRuleKind)999)]
    void should_fail_closed_instead_of_emitting_a_todo(ValidationRuleKind kind)
    {
        var location = SourceLocation.Start;
        var command = new CommandSyntax(
            "Register",
            [new("reference", new("String", false, false, location), location)],
            null,
            [new DeclarativeValidateSyntax([new("reference", kind, new LiteralExpressionSyntax("luhn", location), null, location)], location)],
            [],
            null,
            location);
        var error = Assert.Throws<UnsupportedValidationRule>(() => CommandValidatorRenderer.Render(new CSharpCodeBuilder(), command, "Register", new ApplicationSet([]), []));
        Assert.StartsWith("STAGE-ESM-041:", error.Message, StringComparison.Ordinal);
    }
}
