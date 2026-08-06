// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ExpressionRenderer;

public class when_rendering_an_unsupported_condition : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => ExpressionRenderer.Render(new an_unrecognized_condition(SourceLocation.Start)));

    [Fact] void should_fail_rather_than_render_an_always_passing_guard() => _error.ShouldBeOfExactType<UnsupportedCondition>();
    [Fact] void should_name_the_construct_it_could_not_render() => _error.Message.ShouldContain(nameof(an_unrecognized_condition));

    sealed record an_unrecognized_condition(SourceLocation Location) : ConditionSyntax(Location);
}
