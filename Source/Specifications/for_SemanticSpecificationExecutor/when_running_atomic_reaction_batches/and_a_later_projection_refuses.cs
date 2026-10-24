// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches;

public class and_a_later_projection_refuses : when_settling_reactions.given.a_projected_reaction
{
    int _worldFacts;

    void Establish() => CompileProjected(
        "\"two\"",
        "then no readmodel Marker for \"two\"",
        shape: "literal",
        reaction: "reaction Worker\n  when Started\n    item\n    produces Completed\n    produces Followed\n      item = \"two\"",
        scenario: new() { Policy = "reaction batches atomic" });

    async Task Because()
    {
        var specification = _plan.Specifications.Values.Single();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => _worldFacts = count };
        _result = (await executor.Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    [Fact] void should_refuse_the_projection() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_append_none_of_the_batch() => Assert.True(_worldFacts <= 1, $"{_worldFacts} facts were appended");
}
