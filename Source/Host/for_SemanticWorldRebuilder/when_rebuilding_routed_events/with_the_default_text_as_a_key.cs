// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_the_default_text_as_a_key : given.a_routed_event
{
    SemanticWorld _world = null!;

    void Establish() => _event.Context.EventStreamId = "Default";

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_not_drop_a_key_named_default() => _world.Facts.Single().Route.ShouldEqual(new SemanticEventRoute("stored-account", "stored-transactions", "Default"));
}
