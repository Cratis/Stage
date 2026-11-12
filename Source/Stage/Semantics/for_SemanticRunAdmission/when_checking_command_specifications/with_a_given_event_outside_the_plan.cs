// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission.when_checking_command_specifications;

public class with_a_given_event_outside_the_plan : given.a_run_specification
{
    void Establish()
    {
        Prepare(Invoicing);
        _specification = _specification with { GivenEvents = [_specification.ThenEvents[0] with { EventContract = SemanticId.Create(SemanticKind.EventContract, "Bogus") }] };
    }

    void Because() => Check();

    [Fact] void should_refuse_the_plan_issue() => Refusal().ShouldEqual($"PlanIssue|{SemanticId.Create(SemanticKind.EventContract, "Bogus")}|The Given event is not in the plan.");
}
