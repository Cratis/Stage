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
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using NSubstitute;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_captured_delivery : a_translation_application
{
    private protected readonly a_keyed_capture_log _log = new();
    protected readonly IEventStore _store = Substitute.For<IEventStore>();
    protected readonly IEventSequence _inbox = Substitute.For<IEventSequence>();
    protected readonly List<AppendedEvent> _history = [];

    void Establish()
    {
        _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan()), slice => slice with
        {
            Specifications = [],
            Captures = [.. slice.Captures.Select(capture => capture with
            {
                Appends = [.. capture.Appends.Take(2).Select(append => append with { When = null, Tags = ["captured"] })]
            })]
        });
        _store.EventLog.Returns(_log.Log);
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        _store.GetEventSequence(new EventSequenceId("inbox-carrier")).Returns(_inbox);
        _inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(call =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _history.Where(fact => call.Arg<EventSourceId?>() is null || call.Arg<EventSourceId?>() == EventSourceId.Unspecified || fact.Context.EventSourceId == call.Arg<EventSourceId?>())]));
    }

    protected object Reactor() => Activator.CreateInstance(_assembly.GetType("Contracts.Shipping.Tracking.Receive.Feed", throwOnError: true)!, _store)!;

    protected void MarkSourceKey()
    {
        _model = Transform(_model, slice => slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { EventsSource = capture.EventsSource! with { KeyIsEventSource = true } })]
        });
        _model = ExecutableSemanticModel.Create(LanguageVersion.V33, SemanticVersion.V33, _model.Application);
    }

    protected AppendedEvent Fact(ulong sequence, string status = "sent", string source = "foreign-stream", string key = "parcel") => new(
        EventContext.Empty with { SequenceNumber = sequence, EventSourceId = source },
        Arrived(status, key));

    protected async Task<EventForEventSourceId[]> Effects(AppendedEvent fact)
    {
        var reactor = Reactor();
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnArrived")!.Invoke(reactor, [fact.Content, fact.Context, ReactorDelivery.For(reactor, fact.Context)])!;
        return [.. effects.Cast<EventForEventSourceId>()];
    }

    protected async Task Deliver(AppendedEvent fact)
    {
        var reactor = Reactor();
        var delivery = ReactorDelivery.For(reactor, fact.Context);
        var effects = await Effects(fact);
        if (effects.Length == 0) return;
        var result = await new EventsForEventSourceIdResultHandler().Handle(new ReactorContext(fact.Context, reactor, []) { Delivery = delivery }, _store, effects);
        result.IsSuccess.ShouldBeTrue();
    }
    object Arrived(string status, string key)
    {
        var constructor = _assembly.GetType("Contracts.Shipping.Tracking.Receive.Arrived", throwOnError: true)!.GetConstructors().Single();
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => string.Equals(parameter.Name, "Id", StringComparison.OrdinalIgnoreCase) ? key : status)]);
    }
}
#endif
