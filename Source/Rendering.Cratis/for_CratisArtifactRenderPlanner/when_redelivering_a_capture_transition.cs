// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_redelivering_a_capture_transition : given.a_translation_application
{
    readonly given.a_keyed_capture_log _log = new();
    readonly IEventStore _store = Substitute.For<IEventStore>();
    readonly IEventSequence _inbox = Substitute.For<IEventSequence>();
    readonly List<AppendedEvent> _history = [];

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        _store.EventLog.Returns(_log.Log);
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        _store.GetEventSequence(new EventSequenceId("inbox-carrier")).Returns(_inbox);
        _inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _history]));
    }

    async Task Because()
    {
        Render(clientOnly: true);
        var first = Fact(0, "sent");
        await Deliver(first);
        _history.Add(first);
        var transition = Fact(1, "delivered");
        _history.Add(transition);
        _history.Add(Fact(2, "returned"));
        _log.LoseNextAcknowledgment = true;
        await Assert.ThrowsAsync<given.a_keyed_capture_log.LostAcknowledgment>(() => Deliver(transition));
        await Deliver(transition);
    }

    [Fact] void should_append_the_transition_once_after_a_lost_acknowledgment() => _log.Committed.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Added", "StatusRecorded"]);
    [Fact] void should_retry_the_same_effect_key_from_the_prefix_before_the_delivered_position() => _log.Attempts[1].Single().IdempotencyKey.ShouldEqual(_log.Attempts[2].Single().IdempotencyKey);
    [Fact] void should_preserve_the_transition_value_despite_later_inbox_records() => _log.Committed.Last().Event.GetType().GetProperty("Status")!.GetValue(_log.Committed.Last().Event).ShouldEqual("delivered");
    [Fact] void should_route_the_transition_to_the_record_key_not_the_foreign_stream() => _log.Committed.ShouldEachConformTo(effect => effect.EventSourceId.Value == "parcel");

    AppendedEvent Fact(ulong sequence, string status) => new(
        EventContext.Empty with { SequenceNumber = sequence, EventSourceId = "foreign-stream" },
        Arrived(status));

    object Arrived(string status)
    {
        var constructor = _assembly.GetType("Contracts.Shipping.Tracking.Receive.Arrived", throwOnError: true)!.GetConstructors().Single();
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => string.Equals(parameter.Name, "Id", StringComparison.OrdinalIgnoreCase) ? "parcel" : status)]);
    }

    async Task Deliver(AppendedEvent fact)
    {
        var reactor = Activator.CreateInstance(_assembly.GetType("Contracts.Shipping.Tracking.Receive.Feed", throwOnError: true)!, _store)!;
        var delivery = ReactorDelivery.For(reactor, fact.Context);
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnArrived")!.Invoke(reactor, [fact.Content, fact.Context, delivery])!;
        var result = await new EventsForEventSourceIdResultHandler().Handle(new ReactorContext(fact.Context, reactor, []) { Delivery = delivery }, _store, effects);
        result.IsSuccess.ShouldBeTrue();
    }
}
#endif
