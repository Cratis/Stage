// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches;

public class and_a_refusal_branch_rejects : given.an_observed_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item\n      on refused by validation\n        produces Completed\n        produces Followed\n          item = item",
        "then error \"Already followed\"",
        new()
        {
            Policy = "reaction batches atomic",
            CommandProperties = "validate\n  require item != \"one\"\n    message \"Refused\"",
            Declarations = "constraint Once\n  unique event Followed\n  message \"Already followed\"",
            Caller = "given Followed\n  for \"one\"\n  item = \"one\""
        });

    Task Because() => ExecuteObserved();

    [Fact] void should_reject_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_preserve_the_constraint_name() => _result.Trace!.RejectionCode.ShouldEqual("Once");
    [Fact] void should_append_none_of_the_branch() => _worldFacts.ShouldEqual(2);
}
