// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_event_source_id_mapping : given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    produces Followed\n      item = $eventSourceId",
        "then Followed\n  item = \"one\"");

    Task Because() => Execute();

    [Fact] void should_match_the_reference_contract() => AssertMatchesReference();
    [Fact] void should_pass() => _result.Outcome.ShouldEqual(Cratis.Stage.Contracts.Specifications.Semantic.SemanticSpecificationOutcome.Passed);
}
