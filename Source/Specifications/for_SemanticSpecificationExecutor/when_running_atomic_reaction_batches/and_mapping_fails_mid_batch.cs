// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches;

public class and_mapping_fails_mid_batch : when_settling_reactions.given.a_reaction_plan
{
    const string Message = "Reaction 'Worker' uses $context, which needs the scenario's 'given clock'.";

    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item\n    produces Noted\n      notedAt = $context.occurred",
        $"then error \"{Message}\"",
        new() { Policy = "reaction batches atomic" });

    Task Because() => Execute();

    [Fact] void should_reject_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_report_the_mapping_failure() => _result.Trace!.Rejection.ShouldEqual(Message);
    [Fact] void should_append_none_of_the_batch() => _result.Trace!.Facts.Count.ShouldEqual(1);
}
