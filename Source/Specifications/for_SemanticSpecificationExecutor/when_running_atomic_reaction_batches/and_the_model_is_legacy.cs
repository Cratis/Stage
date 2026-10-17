// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches;

public class and_the_model_is_legacy : given.an_observed_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    produces Completed\n    produces Followed\n      item = item\n    produces Followed\n      item = item",
        "then error \"Already followed\"",
        new() { Declarations = "constraint Once\n  unique event Followed\n  message \"Already followed\"" });

    Task Because() => ExecuteObserved();

    [Fact] void should_reject_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_keep_the_productions_before_the_rejected_one() => _worldFacts.ShouldEqual(3);
}
