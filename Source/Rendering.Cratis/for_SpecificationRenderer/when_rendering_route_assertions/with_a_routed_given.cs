// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_routed_given : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = routed_specifications.Plan(routed_specifications.Compile());
    [Fact] void should_seed_the_route_through_the_log() => routed_specifications.Code(_plan).ShouldContain("await _scenario.EventLog.Append(\"history\", new global::Routed.Banking.Deposits.Deposit.Historical(), eventSourceType: \"stored-account\", eventStreamType: \"stored-transactions\", eventStreamId: \"previous\")");
    [Fact] void should_exclude_seeded_facts_from_the_assertion() => routed_specifications.Code(_plan).ShouldContain("_givenEventCount + 0");
    [Fact] void should_check_that_seeding_succeeded() => routed_specifications.Code(_plan).ShouldContain(").IsSuccess.ShouldBeTrue();");
}
