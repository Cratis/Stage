// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_occurred_mapping_without_a_clock : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    produces Noted\n      notedAt = $context.occurred", "then error \"Reaction 'Worker' uses $context, which needs the scenario's 'given clock'.\"");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_contract_rejection() => AssertMatchesReference();
    [Fact] void should_not_supply_the_executor_clock_as_a_fixture() => _result.Trace!.Rejection.ShouldEqual("Reaction 'Worker' uses $context, which needs the scenario's 'given clock'.");
}
