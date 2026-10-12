// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_routed_reaction_cause : given.a_persisted_reaction_world
{
    protected override bool Routed => true;
    protected override bool ExplicitDestination => false;

    void Because() => _rebuilt = SemanticWorldRebuilder.Create(_plan, _history, 1);

    [Fact] void should_restore_the_durable_live_facts() => DurableFacts(_rebuilt).ShouldEqual(DurableFacts(_live));
    [Fact] void should_restore_the_projected_live_world() => ReadModels(_rebuilt).ShouldEqual(ReadModels(_live));
    [Fact] void should_restore_the_causes_route() => _rebuilt.Facts[0].Route.ShouldNotBeNull();
    [Fact] void should_keep_the_reaction_production_unrouted_like_settlement() => _rebuilt.Facts[1].Route.ShouldBeNull();
    [Fact] void should_not_react_to_the_history_again() => _rebuilt.Facts.Length.ShouldEqual(2);
}
