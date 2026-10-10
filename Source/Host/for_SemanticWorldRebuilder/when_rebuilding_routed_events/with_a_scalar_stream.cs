// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_a_scalar_stream : given.a_routed_event
{
    SemanticWorld _world = null!;

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_reconstruct_the_stored_route() => _world.Facts.Single().Route.ShouldEqual(new SemanticEventRoute("stored-account", "stored-transactions", "2026-10"));
    [Fact] void should_preserve_the_source_identity() => _world.Facts.Single().Destination.ShouldEqual(SemanticValue.Text("acc-1"));
}
