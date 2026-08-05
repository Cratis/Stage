// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ExpressionRenderer;

public class when_rendering_conditions : Specification
{
    string _comparison = null!;
    string _logical = null!;

    void Because()
    {
        var comparison = new ComparisonConditionSyntax(
            "status",
            ComparisonOperator.Equal,
            new LiteralExpressionSyntax("sent", SourceLocation.Start),
            SourceLocation.Start);

        _comparison = ExpressionRenderer.Render(comparison);

        _logical = ExpressionRenderer.Render(new LogicalConditionSyntax(
            comparison,
            LogicalOperator.Or,
            new ComparisonConditionSyntax("isProForma", ComparisonOperator.Equal, new LiteralExpressionSyntax(true, SourceLocation.Start), SourceLocation.Start),
            SourceLocation.Start));
    }

    [Fact] void should_render_a_comparison_condition() => _comparison.ShouldEqual("Status == \"sent\"");
    [Fact] void should_render_a_logical_condition_combining_both_sides() => _logical.ShouldEqual("(Status == \"sent\") || (IsProForma == true)");
}
