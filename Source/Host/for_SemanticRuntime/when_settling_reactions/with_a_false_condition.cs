// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_false_condition : a_reaction_runtime
{
    protected override string Model => StartSource + ProducingSource.Replace("reaction CompleteWork", "reaction CompleteWork\n        where amount < 0", StringComparison.Ordinal);

    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_only_the_initiating_fact() => FactNames(_batches.Single()).ShouldContainOnly(["Started"]);
    [Fact] void should_leave_the_reaction_unfired() => _result.World.Facts.Length.ShouldEqual(1);
}
