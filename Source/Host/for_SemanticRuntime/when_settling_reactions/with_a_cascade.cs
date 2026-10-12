// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_cascade : a_reaction_runtime
{
    protected override string Model => StartSource + InvokingSource + """

              reaction Finish
                when Completed
                  id
                  produces Finished
                    for id
              event Finished
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_cascade() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_every_cascade_fact_in_one_batch() => FactNames(_batches.Single()).ToArray().ShouldEqual(["Started", "Completed", "Finished"]);
    [Fact] void should_commit_the_settled_world() => _result.World.Facts.Length.ShouldEqual(3);
}
