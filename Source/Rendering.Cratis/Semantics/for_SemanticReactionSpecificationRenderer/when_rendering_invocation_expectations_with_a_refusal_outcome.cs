// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_invocation_expectations_with_a_refusal_outcome : given.a_refusal_outcome_with_invocation_expectations
{
    void Because()
    {
        ValidateExpectations();
        Validate();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_run_the_invoked_command_so_its_refusal_comes_from_the_given_state() => _source.ShouldContain("executor.Execute(effects, reactorContext, eventStore)");
    [Fact] void should_record_the_effects_unwrapped_from_their_command_invocations() => _source.ShouldContain("_effects = [.. effects.Select(produced => produced is CommandInvocation invocation ? invocation.Command : produced)];");
    [Fact] void should_settle_the_refusal_branch() => _source.ShouldContain("should_settle() => _settled.ShouldBeTrue()");
    [Fact] void should_append_nothing_after_the_trigger() => _source.ShouldContain("_facts.Length.ShouldEqual(0)");
    [Fact] void should_compare_the_recorded_count() => _source.ShouldContain("should_match_authored_invocation_count() => _effects.Count.ShouldEqual(1)");
    [Fact] void should_compare_the_recorded_command_value() => _source.ShouldContain("should_match_authored_invocation_1_id()");
    [Fact] void should_not_use_the_recording_reactor_scenario() => _source.ShouldNotContain("ReactorScenario");
}
#endif
