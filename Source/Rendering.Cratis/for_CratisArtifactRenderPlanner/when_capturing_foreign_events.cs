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

    async Task Because()
    {
        Render();
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        await Deliver(First);
        await Deliver(First);
        await Deliver(Second);
    }

    [Fact] void should_append_a_local_fact_for_each_unconditional_occurrence() => _effects.Count.ShouldEqual(3);
    [Fact] void should_derive_the_local_destination_from_the_capture_key_not_the_inbox_stream() => _effects.Select(effect => effect.EventSourceId.Value).ShouldEqual([First, First, Second]);
    [Fact] void should_copy_the_foreign_payload_into_the_local_fact() => _effects.Select(effect => effect.Event.GetType().GetProperty("OrderId")!.GetValue(effect.Event)!.ToString()).ShouldEqual([First, First, Second]);
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
    public async Task should_approximate_added_with_existing_local_facts_for_the_key_and_target_type()
    {
        _model = Transform(_model, slice => slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.Added, []) })] })]
        });
        Render();
        _effects.Clear();
        _store.EventLog.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(call =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _effects.Where(effect => effect.EventSourceId == call.Arg<EventSourceId>()).Select(effect => new AppendedEvent(EventContext.EmptyWithEventSourceId(effect.EventSourceId), effect.Event))]));
        await Deliver(First);
        await Deliver(First);
        await Deliver(Second);
        _effects.Select(effect => effect.EventSourceId.Value).ShouldEqual([First, Second]);
    }

    object NewReactor()
    {
        var type = Generated("TrackShipments", "ShipmentTracking");
        return type.GetConstructors().Single().GetParameters().Length == 0 ? Activator.CreateInstance(type)! : Activator.CreateInstance(type, _store)!;
    }

    async Task Deliver(string key)
    {
        var keyType = _assembly.GetType("Contracts.Common.OrderId", throwOnError: true)!;
        var source = Activator.CreateInstance(Generated("TrackShipments", "ShipmentDispatched"), Activator.CreateInstance(keyType, Guid.Parse(key)))!;
        var reactor = NewReactor();
        var result = reactor.GetType().GetMethod("OnShipmentDispatched")!.Invoke(reactor, [source])!;
        var effects = result is Task<IEnumerable<object>> pending ? await pending : (IEnumerable<object>)result;
        _effects.AddRange(effects.Cast<EventForEventSourceId>());
    }
}
#endif
