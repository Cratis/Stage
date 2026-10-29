// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_seeded_nondecision_refusal_history : given.a_refusal_specification
{
    void Establish() => Prepare(RefusalSource
        .Replace("      specification Completing", "      specification Completing\n        given Seeded\n          for \"one\"", StringComparison.Ordinal));

    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_scenario_typed_by_the_command() => _source.ShouldContain("CommandScenario<Complete> _scenario");
    [Fact] void should_append_the_seed_and_assert_its_status() => _source.ShouldContain("(await _scenario.EventLog.Append(_id, new Seeded())).IsSuccess.ShouldBeTrue();");
    [Fact] void should_seed_before_appending_the_trigger() => _source.IndexOf("new Seeded()", StringComparison.Ordinal).ShouldBeLessThan(_source.IndexOf("_scenario.EventLog.Append(_id, @event)", StringComparison.Ordinal));
    [Fact] void should_not_use_decision_given_seeding() => _source.ShouldNotContain("_scenario.Given");
    [Fact] void should_not_use_the_legacy_event_scenario() => _source.ShouldNotContain("EventScenario");
}
