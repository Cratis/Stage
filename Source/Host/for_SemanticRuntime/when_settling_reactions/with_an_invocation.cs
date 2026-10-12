// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_invocation : a_reaction_runtime
{
    protected override string Model => StartSource + InvokingSource;

    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_invoked_commands_fact_in_the_same_batch() => FactNames(_batches.Single()).ShouldContainOnly(["Started", "Completed"]);
    [Fact] void should_commit_the_invoked_world() => _result.World.Facts.Length.ShouldEqual(2);
}
