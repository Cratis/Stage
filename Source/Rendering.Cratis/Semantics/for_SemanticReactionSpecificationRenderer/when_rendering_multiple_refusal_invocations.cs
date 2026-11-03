// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_multiple_refusal_invocations : given.a_refusal_specification
{
    void Establish() => Prepare(RefusalSource
        .Replace("      event Started", "      command FollowUp\n        id String identifier\n        produces Followed\n          for id\n      event Followed\n      event Started", StringComparison.Ordinal)
        .Replace("      specification Completing", "          invokes FollowUp\n            id = id\n      specification Completing\n        given Seeded\n          for \"one\"", StringComparison.Ordinal)
        .Replace("        then Completed\n          for \"one\"", "        then Completed\n          for \"one\"\n        then Followed\n          for \"one\"", StringComparison.Ordinal));

    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_leave_decision_reads_to_the_scenario_on_the_untyped_scenario() => _source.ShouldContain("CommandScenario<object> _scenario = new CommandScenario<object>();");
    [Fact] void should_not_call_the_obsolete_decision_reads_switch() => _source.ShouldNotContain("UseDecisionReads");
    [Fact] void should_import_the_decision_reads_extension_namespace() => _source.ShouldContain("using Cratis.Arc.Chronicle.Testing.Commands;");
    [Fact] void should_seed_history() => _source.ShouldContain("new Seeded()");
    [Fact] void should_execute_the_untyped_commands() => _source.ShouldContain("_scenario.Execute(call.ArgAt<object>(0))");
    [Fact] void should_not_use_the_legacy_event_scenario() => _source.ShouldNotContain("EventScenario");
}
