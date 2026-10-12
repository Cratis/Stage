// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_non_matching_route_filter : a_reaction_runtime
{
    protected override string Model => "eventsource Account\n  stream Notes\n" + StartSource + ProducingSource.Replace("reaction CompleteWork", "reaction CompleteWork\n        from Account.Notes", StringComparison.Ordinal);

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_reaction_filter() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_fire_for_an_unrouted_fact() => FactNames(_batches.Single()).ShouldContainOnly(["Started"]);
}
