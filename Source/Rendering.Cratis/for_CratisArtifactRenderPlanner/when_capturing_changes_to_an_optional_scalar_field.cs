// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_capturing_changes_to_an_optional_scalar_field : given.a_translation_application
{
    const string NullToValue = "1fa85f64-5717-4562-b3fc-2c963f66afa1";
    const string ValueToNull = "2fa85f64-5717-4562-b3fc-2c963f66afa2";
    const string NullToNull = "3fa85f64-5717-4562-b3fc-2c963f66afa3";
    const string ValueToSameValue = "4fa85f64-5717-4562-b3fc-2c963f66afa4";
    readonly List<EventForEventSourceId> _effects = [];
    readonly IEventStore _store = Substitute.For<IEventStore>();
    readonly IEventSequence _inbox = Substitute.For<IEventSequence>();
    readonly List<AppendedEvent> _delivered = [];

    void Establish() => _model = Transform(_model, slice => slice.Name != "TrackShipments" ? slice : slice with
    {
        Events = [.. slice.Events.Select(contract => contract.Name != "ShipmentDispatched" ? contract : contract with
        {
            Properties = [.. contract.Properties, contract.Properties.Single() with
            {
                Id = SemanticId.Create(SemanticKind.Property, "carrier"),
                Name = "carrier",
                Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) with { IsOptional = true }
            }]
        })],
        Captures = [.. slice.Captures.Select(capture => capture with
        {
            Appends = [.. capture.Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.AnyChanged, ["carrier"]) })]
        })]
    });

    async Task Because()
    {
        Render();
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        _store.GetEventSequence(new EventSequenceId("inbox-shipping")).Returns(_inbox);
        _inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _delivered]));
        await Deliver(NullToValue, null);
        await Deliver(NullToValue, "DHL");
        await Deliver(ValueToNull, "DHL");
        await Deliver(ValueToNull, null);
        await Deliver(NullToNull, null);
        await Deliver(NullToNull, null);
        await Deliver(ValueToSameValue, "DHL");
        await Deliver(ValueToSameValue, "DHL");
    }

    [Fact] void should_append_when_the_field_goes_from_null_to_a_value() => AppendsFor(NullToValue).ShouldEqual(1);
    [Fact] void should_append_when_the_field_goes_from_a_value_to_null() => AppendsFor(ValueToNull).ShouldEqual(1);
    [Fact] void should_append_nothing_when_the_field_stays_null() => AppendsFor(NullToNull).ShouldEqual(0);
    [Fact] void should_append_nothing_when_the_field_keeps_its_value() => AppendsFor(ValueToSameValue).ShouldEqual(0);

    int AppendsFor(string key) => _effects.Count(effect => effect.EventSourceId.Value == key);

    async Task Deliver(string key, string? carrier)
    {
        var source = Activator.CreateInstance(Generated("TrackShipments", "ShipmentDispatched"), Activator.CreateInstance(_assembly.GetType("Contracts.Common.OrderId", throwOnError: true)!, Guid.Parse(key)), carrier)!;
        var sequenceNumber = (ulong)_delivered.Count;
        _delivered.Add(new(EventContext.Empty with { SequenceNumber = sequenceNumber, EventType = source.GetType().GetEventType() }, source));
        var reactor = Activator.CreateInstance(Generated("TrackShipments", "ShipmentTracking"), _store)!;
        var context = EventContext.Empty with { SequenceNumber = sequenceNumber };
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnShipmentDispatched")!.Invoke(reactor, [source, context])!;
        _effects.AddRange(effects.Cast<EventForEventSourceId>());
    }
}
#endif
