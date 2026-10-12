// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_constraint_violation : a_reaction_runtime
{
    protected override string Model => StartSource + ProducingSource + """

              constraint OnlyCompletedOnce
                unique event Completed
                message "Already completed"
        """;

    async Task Establish()
    {
        await Execute();
        _batches.Clear();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_reaction_production() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_append_neither_fact() => _batches.ShouldBeEmpty();
    [Fact] void should_retain_only_the_prior_committed_world() => FactNames(_result.World.Facts).ToArray().ShouldEqual(["Started", "Completed"]);
}
