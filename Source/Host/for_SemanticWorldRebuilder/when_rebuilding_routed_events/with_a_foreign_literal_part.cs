// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_a_foreign_literal_part : given.a_routed_event
{
    Exception? _error;

    void Establish()
    {
        _plan = compiled_plan.From(with_a_composite_stream_id.CompositeSource);
        _event.Context.EventStreamId = "2026-10|2027";
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_a_part_no_producer_could_write() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
