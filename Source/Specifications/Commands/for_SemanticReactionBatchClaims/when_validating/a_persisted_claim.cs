// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticReactionBatchClaims.when_validating;

public class a_persisted_claim : given.a_constrained_plan
{
    void Because() => Validate([Changed("a", "first"), Claimed("a")], Changed("b", "first"), Claimed("a"));

    [Fact] void should_refuse_another_owner_holding_the_value() => _results[0]!.Name.ShouldEqual("UniqueValue");
    [Fact] void should_refuse_a_claim_the_history_holds_open() => _results[1]!.Name.ShouldEqual("OneClaim");
}
