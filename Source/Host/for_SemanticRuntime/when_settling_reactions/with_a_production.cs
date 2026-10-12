// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_production : a_reaction_runtime
{
    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_both_facts_in_one_batch() => FactNames(_batches.Single()).ShouldContainOnly(["Started", "Completed"]);
    [Fact] void should_return_both_facts_in_append_order() => FactNames(((SemanticAccepted)_result).Facts).ToArray().ShouldEqual(["Started", "Completed"]);
    [Fact] void should_commit_the_settled_world() => _result.World.Facts.Length.ShouldEqual(2);
    [Fact] void should_guard_the_whole_batch_at_the_empty_tail() => _expectedTails.Single().ShouldEqual(ulong.MaxValue);
    [Fact] void should_retain_the_mapped_value() => _batches.Single()[1].Values.Single(value => value.TargetProperty == _plan.Events.Values.Single(contract => contract.Name == "Completed").Properties.Single(property => property.Name == "amount").Id).Value.ShouldEqual(SemanticValue.Number(7));
    [Fact] void should_mark_the_reaction_origin() => _batches.Single()[1].ReactionOrigin.ShouldEqual(_plan.Reactions.Single().Id);
}
