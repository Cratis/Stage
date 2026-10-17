// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation.for_InvariantValidationInterpolation;

public class when_rendering_validation_bodies : Specification
{
    [Theory]
    [InlineData("const string message = $\"constant\"; yield return message;", "const string message = $\"constant\"; yield return message;")]
    [InlineData("yield return $\"{amount,12:N2}|{date:yyyy-MM-dd}\";", "yield return (global::System.FormattableString.Invariant($\"{amount,12:N2}|{date:yyyy-MM-dd}\"));")]
    [InlineData("yield return $\"{$\"{amount:F2}\"}|{date:yyyy-MM-dd}\";", "yield return (global::System.FormattableString.Invariant($\"{(global::System.FormattableString.Invariant($\"{amount:F2}\"))}|{date:yyyy-MM-dd}\"));")]
    [InlineData("yield return $@\"{{value}} {amount:N2}\";", "yield return (global::System.FormattableString.Invariant($@\"{{value}} {amount:N2}\"));")]
    void should_wrap_the_original_expression_without_changing_holes(string body, string expected)
    {
        var rendered = InvariantValidationInterpolation.Render(body);
        rendered.ShouldEqual(expected);
        SyntaxFactory.ParseStatement("{\n" + rendered + "\n}", consumeFullText: true).ContainsDiagnostics.ShouldBeFalse();
    }
}
#endif
