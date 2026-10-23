// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_capturing_foreign_events : given.a_translation_application
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
    readonly List<EventForEventSourceId> _effects = [];
    readonly IEventStore _store = Substitute.For<IEventStore>();
    readonly IEventSequence _inbox = Substitute.For<IEventSequence>();
    readonly List<AppendedEvent> _delivered = [];

    async Task Because()
    {
        Render();
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        _store.GetEventSequence(new EventSequenceId("inbox-shipping")).Returns(_inbox);
        _inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _delivered]));
        await Deliver(First);
        await Deliver(First);
        await Deliver(Second);
    }

    [Fact] void should_append_nothing_for_an_unchanged_foreign_record() => _effects.Count.ShouldEqual(2);
    [Fact] void should_derive_the_local_destination_from_the_capture_key_not_the_inbox_stream() => _effects.Select(effect => effect.EventSourceId.Value).ShouldEqual([First, Second]);
    [Fact] void should_copy_the_foreign_payload_into_the_local_fact() => _effects.Select(effect => effect.Event.GetType().GetProperty("OrderId")!.GetValue(effect.Event)!.ToString()).ShouldEqual([First, Second]);
    [Fact] void should_read_capture_state_from_the_inbox_it_observes() => _inbox.ReceivedCalls().ShouldNotBeEmpty();
    [Fact] void should_produce_only_local_private_facts() => _effects.ShouldEachConformTo(effect => effect.Event.GetType() == Generated("TrackShipments", "OrderDispatched"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_append_the_capture_effects_to_the_local_log_and_propagate_its_result(bool refused)
    {
        var log = Substitute.For<IEventLog>();
        _store.EventLog.Returns(log);
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(refused
            ? AppendManyResult.Failed(CorrelationId.New(), [new AppendError("Refused")])
            : AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]));
        var context = new ReactorContext(EventContext.Empty, NewReactor(), []);
        var handler = new EventsForEventSourceIdResultHandler();
        var result = await handler.Handle(context, _store, _effects.ToArray());
        result.IsSuccess.ShouldEqual(!refused);
        await log.Received(1).AppendMany(Arg.Is<IEnumerable<EventForEventSourceId>>(effects => effects.SequenceEqual(_effects)));
    }

    [Fact]
    public async Task should_ignore_inbox_records_after_the_delivered_position_so_replayed_state_is_deterministic()
    {
        _effects.Clear();
        var reactor = NewReactor();
        await Invoke(reactor, Dispatched(First), sequenceNumber: 0);
        _effects.Select(effect => effect.EventSourceId.Value).ShouldEqual([First]);
    }

    object NewReactor() => Activator.CreateInstance(Generated("TrackShipments", "ShipmentTracking"), _store)!;

    object Dispatched(string key) =>
        Activator.CreateInstance(Generated("TrackShipments", "ShipmentDispatched"), Activator.CreateInstance(_assembly.GetType("Contracts.Common.OrderId", throwOnError: true)!, Guid.Parse(key)))!;

    async Task Deliver(string key)
    {
        var source = Dispatched(key);
        var sequenceNumber = (ulong)_delivered.Count;
        _delivered.Add(new(EventContext.Empty with { SequenceNumber = sequenceNumber, EventType = source.GetType().GetEventType() }, source));
        await Invoke(NewReactor(), source, sequenceNumber);
    }

    async Task Invoke(object reactor, object source, ulong sequenceNumber)
    {
        var context = EventContext.Empty with { SequenceNumber = sequenceNumber };
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnShipmentDispatched")!.Invoke(reactor, [source, context])!;
        _effects.AddRange(effects.Cast<EventForEventSourceId>());
    }
}
#endif
