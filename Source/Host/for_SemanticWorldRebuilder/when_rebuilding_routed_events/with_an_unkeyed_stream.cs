// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_an_unkeyed_stream : given.a_routed_event
{
    SemanticWorld _world = null!;

    void Establish()
    {
        _plan = compiled_plan.From(a_routed_runtime.Source.Replace("    streamId Period\n", string.Empty, StringComparison.Ordinal).Replace("          streamId = period\n", string.Empty, StringComparison.Ordinal));
        _event.Context.EventStreamId = "Default";
    }

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_restore_an_unkeyed_route() => _world.Facts.Single().Route.ShouldEqual(new SemanticEventRoute("stored-account", "stored-transactions", null));
}
