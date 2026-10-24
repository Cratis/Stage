// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_source_key : given.a_captured_delivery
{
    void Establish() => MarkSourceKey();
    void Because() => Render();

    [Fact]
    public async Task should_read_only_the_delivered_source_and_captured_types()
    {
        _history.Add(Fact(0, source: "parcel"));
        _history.Add(Fact(1, "unrelated", source: "another"));
        var effects = await Effects(Fact(2, source: "parcel"));
        effects.ShouldBeEmpty();
        var expectedTypes = new[] { _history[0].Content.GetType().GetEventType(), _assembly.GetType("Contracts.Shipping.Tracking.Receive.Labelled", true)!.GetEventType() };
        await _inbox.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, new EventSourceId("parcel"),
            Arg.Is<IEnumerable<EventType>>(types => types.Count() == expectedTypes.Length && expectedTypes.All(types.Contains)));
    }

    [Fact]
    public async Task should_use_only_facts_strictly_before_the_delivery_in_sequence_order()
    {
        _history.Add(Fact(4, "after", source: "parcel"));
        _history.Add(Fact(3, "equal", source: "parcel"));
        _history.Add(Fact(2, "sent", source: "parcel"));
        _history.Add(Fact(0, "earlier", source: "parcel"));
        (await Effects(Fact(3, source: "parcel"))).ShouldBeEmpty();
    }
}
#endif
