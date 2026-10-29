// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_a_single_unprotected_refusal_invocation : given.a_refusal_specification
{
    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_type_the_scenario_by_the_actual_invoked_command() => _source.ShouldContain("CommandScenario<Complete> _scenario = new();");
    [Fact] void should_not_use_an_untyped_scenario() => _source.ShouldNotContain("CommandScenario<object>");
    [Fact] void should_execute_the_concrete_command() => _source.ShouldContain("_scenario.Execute((Complete)call.ArgAt<object>(0))");
    [Fact] void should_not_use_the_legacy_event_scenario() => _source.ShouldNotContain("EventScenario");
}
