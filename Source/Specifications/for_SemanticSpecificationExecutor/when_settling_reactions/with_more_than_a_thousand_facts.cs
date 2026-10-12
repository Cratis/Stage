// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_more_than_a_thousand_facts : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    amount\n    produces Started\n      item = item\n      amount = amount", "");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_fact_limit() => AssertMatchesReference();
    [Fact] void should_report_the_reaction_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Reaction);
    [Fact] void should_report_the_limit() => _result.Unsupported!.Details.ShouldEqual("The scenario appends more than 1000 facts; its reactions do not settle.");
}
