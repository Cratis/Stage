// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_invocation_expectations_with_refusal_branches : given.an_invocation_expectation
{
    void Because()
    {
        var invocation = _located.Slice.Reactions.Single().Triggers.Single().Invokes.Single();
        ChangeTrigger(candidate => candidate with { Invokes = [invocation with { OnRefused = [new("validation", null, [])] }] });
        ValidateExpectations();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_unwrap_the_recorded_command_invocations() => _source.ShouldContain("produced is CommandInvocation invocation ? invocation.Command : produced");
    [Fact] void should_compare_the_recorded_count() => _source.ShouldContain("should_match_authored_invocation_count()");
    [Fact] void should_compare_the_recorded_command_type() => _source.ShouldContain("should_match_authored_invocation_1_command()");
    [Fact] void should_compare_the_stated_reason() => _source.ShouldContain("should_match_authored_invocation_1_reason()");
    [Fact] void should_not_assert_the_invoked_command_succeeded() => _source.ShouldNotContain("should_succeed");
    [Fact] void should_not_execute_the_refusal() => _source.ShouldNotContain("ICommandSideEffectExecutor");
    [Fact] void should_not_settle() => _source.ShouldNotContain("should_settle");
}
#endif
