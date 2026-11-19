// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_invocation_expectations_with_a_constraint_target_append : given.a_constraint_target_with_invocation_expectations
{
    void Because()
    {
        ValidateExpectations();
        Validate();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_append_the_trigger_through_the_event_log_so_the_constraint_is_enforced() => _source.ShouldContain("(await _scenario.EventLog.Append(");
    [Fact] void should_require_the_append_to_be_accepted() => _source.ShouldContain(".IsSuccess.ShouldBeTrue();");
    [Fact] void should_record_the_effects_the_reactor_returned() => _source.ShouldContain("_effects = [.. effects.Select(");
    [Fact] void should_compare_the_recorded_command_value() => _source.ShouldContain("should_match_authored_invocation_1_id()");
    [Fact] void should_not_run_the_invoked_command() => _source.ShouldNotContain("CommandSideEffectExecutor");
    [Fact] void should_not_assert_the_invoked_commands_outcome() => _source.ShouldNotContain("should_settle");
    [Fact] void should_not_use_the_reactor_scenario_that_skips_constraints() => _source.ShouldNotContain("ReactorScenario");
}
#endif
