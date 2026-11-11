// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityReference;

public class when_naming_the_capability : Specification
{
    AbilityReference _reference = null!;
    bool _parsed;
    string _system = null!;
    string _ability = null!;
    bool _parsedAnotherCapability;

    void Because()
    {
        _reference = new("Slack", "Post", 3);
        _parsed = AbilityReference.TryParse(_reference.CapabilityId, out _system, out _ability);
        _parsedAnotherCapability = AbilityReference.TryParse("stage.send.ChargeFee", out _, out _);
    }

    [Fact] void should_name_the_capability_with_the_contract_major() => _reference.Capability.ShouldEqual("ability:Slack.Post@3");
    [Fact] void should_leave_the_version_out_of_the_capability_id() => _reference.CapabilityId.ShouldEqual("ability:Slack.Post");
    [Fact] void should_run_each_invocation_as_a_workflow_named_after_the_capability_id() => _reference.Workflow.ShouldEqual("ability:Slack.Post");
    [Fact] void should_parse_the_system_and_the_ability_back() => (_parsed && _system == "Slack" && _ability == "Post").ShouldBeTrue();
    [Fact] void should_not_parse_a_capability_that_is_not_an_ability() => _parsedAnotherCapability.ShouldBeFalse();
}
