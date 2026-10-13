// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_productions : given.a_reaction_specification
{
    void Because() => Render();
    [Fact] void should_assert_the_exact_count() => _source.ShouldContain("_reactor.ProducedEvents.Count.ShouldEqual(1)");
    [Fact] void should_assert_the_mapped_destination() => _source.ShouldContain("_reactor.ProducedEvents[0].EventSourceId == \"one\"");
    [Fact] void should_assert_the_unrouted_output() => _source.ShouldContain("EventStreamType == global::Cratis.Chronicle.Events.EventStreamType.All");
    [Fact] void should_assert_the_payload() => _source.ShouldContain("@event.Amount == 5m");

    [Fact]
    public void should_render_multiset_assignment_for_any_order()
    {
        _specification = _specification with { ThenEventsInAnyOrder = true };
        Render();
        _source.ShouldContain("Assign(expectation, new bool[actual.Length]).ShouldBeTrue()");
    }
}
