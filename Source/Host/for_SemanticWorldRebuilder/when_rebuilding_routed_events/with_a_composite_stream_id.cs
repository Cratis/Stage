// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_a_composite_stream_id : given.a_routed_event
{
    internal static string CompositeSource => a_routed_runtime.Source
        .Replace("concept Period : String", "concept Period : String\nconcept Year : Int", StringComparison.Ordinal)
        .Replace("    streamId Period", "    streamId\n      period Period\n      year Year", StringComparison.Ordinal)
        .Replace("          streamId = period", "          streamId\n            period = period\n            year = 2026", StringComparison.Ordinal);
    SemanticWorld _world = null!;

    void Establish()
    {
        _plan = compiled_plan.From(CompositeSource);
        _event.Context.EventStreamId = "2026-10|2026";
    }

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_reconstruct_the_composite_route() => _world.Facts.Single().Route.ShouldEqual(new SemanticEventRoute("stored-account", "stored-transactions", "2026-10|2026"));
}
