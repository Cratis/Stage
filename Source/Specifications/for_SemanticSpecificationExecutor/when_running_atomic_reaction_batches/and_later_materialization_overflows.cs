// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches;

public class and_later_materialization_overflows : given.an_observed_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    produces Completed\n    produces Counted\n      count = 9223372036854775808",
        "then no readmodel Marker for \"one\"",
        new() { Policy = "reaction batches atomic", Declarations = "event Counted\n  count Int" });

    async Task Because()
    {
        var specification = _plan.Specifications.Values.Single();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => _worldFacts = count };
        _result = (await executor.Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    [Fact] void should_report_the_execution_failure() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_report_the_overflow() => _result.Failures.ShouldContain("Value was either too large or too small for an Int64.");
    [Fact] void should_append_none_of_the_batch() => _worldFacts.ShouldEqual(1);
}
