// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_an_unrouted_event : given.a_rebuildable_world
{
    SemanticWorld _world = null!;

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_keep_the_default_triple_unrouted() => _world.Facts.Single().Route.ShouldBeNull();
}
