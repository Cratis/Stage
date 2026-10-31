// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
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
        var first = Fact(0, source: "parcel");
        var (effects, inbox) = await EffectsFromHistory(Fact(2, source: "parcel"), [first, Fact(1, "unrelated", source: "another")]);
        effects.ShouldBeEmpty();
        var expectedTypes = new[] { first.Content.GetType().GetEventType(), _assembly.GetType("Contracts.Shipping.Tracking.Receive.Labelled", true).GetEventType() };
        await inbox.Received(1).GetFromSequenceNumber(
            EventSequenceNumber.First,
            new EventSourceId("parcel"),
            Arg.Is<IEnumerable<EventType>>(types => types.Count() == expectedTypes.Length && expectedTypes.All(types.Contains)));
    }

    [Fact]
    public async Task should_use_only_facts_strictly_before_the_delivery_in_sequence_order()
    {
        var (effects, _) = await EffectsFromHistory(Fact(3, source: "parcel"), [Fact(4, "after", source: "parcel"), Fact(3, "equal", source: "parcel"), Fact(2, "sent", source: "parcel"), Fact(0, "earlier", source: "parcel")]);
        effects.ShouldBeEmpty();
    }

    async Task<(EventForEventSourceId[] Effects, IEventSequence Inbox)> EffectsFromHistory(AppendedEvent fact, IEnumerable<AppendedEvent> history)
    {
        var store = Substitute.For<IEventStore>();
        var inbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(new EventSequenceId("inbox-carrier")).Returns(inbox);
        store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(call =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. history.Where(item => call.Arg<EventSourceId?>() is null || call.Arg<EventSourceId?>() == EventSourceId.Unspecified || item.Context.EventSourceId == call.Arg<EventSourceId?>())]));
        var reactor = Activator.CreateInstance(_assembly.GetType("Contracts.Shipping.Tracking.Receive.Feed", throwOnError: true), store);
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnArrived").Invoke(reactor, [fact.Content, fact.Context, ReactorDelivery.For(reactor, fact.Context)]);
        return ([.. effects.Cast<EventForEventSourceId>()], inbox);
    }
}
#endif
