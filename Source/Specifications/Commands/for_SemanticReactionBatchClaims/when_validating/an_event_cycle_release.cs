// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticReactionBatchClaims.when_validating;

public class an_event_cycle_release : given.a_constrained_plan
{
    void Because() => Validate([Claimed("a")], Released("a"), Claimed("a"), Claimed("a"));

    [Fact] void should_accept_the_release() => _results[0].ShouldBeNull();
    [Fact] void should_accept_the_claim_after_the_release() => _results[1].ShouldBeNull();
    [Fact] void should_refuse_a_second_claim_in_the_same_cycle() => _results[2]!.Name.ShouldEqual("OneClaim");
}
