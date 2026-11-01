// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticInbox;

// Runs only when STAGE_CHRONICLE_URL names a Chronicle (for example chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35213); without it the spec has nothing to prove against.
public class when_a_foreign_event_arrives_through_an_inbox : Specification
{
    const string Producer = """
        module Shipping
          feature Dispatching
            slice StateChange Dispatch
              command Dispatch
                orderId String identifier
                produces OrderPacked
                  for orderId
                  orderId = orderId
              event OrderPacked
                orderId String
            slice Translate PublishShipment
              direction outbound
              public event ShipmentDispatched
                orderId String
              projection ShipmentPublisher => ShipmentDispatched
                from OrderPacked
                  orderId = orderId
        """;

    const string Consumer = """
        module Tracking
          feature Receiving
            slice Translate TrackShipments
              direction inbound
              event ShipmentDispatched from "{producer}"
                orderId String
              event OrderDispatched
                orderId String
              capture ShipmentTracking
                source events
                  from ShipmentDispatched
                key orderId
                append OrderDispatched
                  orderId = $.orderId
        """;

    readonly string _url = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_URL") ?? string.Empty;
    ulong _consumerLogTail = ulong.MaxValue;
    bool _ran;

    async Task Because()
    {
        if (_url.Length == 0)
        {
            return;
        }

        var suffix = Guid.NewGuid().ToString("N");
        var producerName = "StageProducer" + suffix;
        var consumerName = "StageConsumer" + suffix;
        using var client = new ChronicleClient(new ChronicleOptions(_url));
        var producerPlan = for_SemanticRuntime.given.compiled_plan.From(Producer);
        var consumerPlan = for_SemanticRuntime.given.compiled_plan.From(Consumer.Replace("{producer}", producerName));
        await SemanticChronicleRegistration.Register(client, producerName, producerPlan);
        var runtime = SemanticRuntimeHosting.Create(consumerPlan, new SemanticFactAppender(client, new StageEventStoreName(consumerName), consumerPlan));
        await SemanticChronicleRegistration.Register(client, consumerName, consumerPlan);
        await SemanticInbox.Start(client, consumerName, consumerPlan, (ISemanticForeignFacts)runtime);

        var packed = producerPlan.Events.Values.Single(contract => contract.Name == "OrderPacked");
        var shipped = producerPlan.Events.Values.Single(contract => contract.Name == "ShipmentDispatched");
        await new SemanticFactAppender(client, new StageEventStoreName(producerName), producerPlan).Append(
            [
                new(packed.Id, SemanticValue.Text("order-1"), [new(packed.Properties.Single().Id, SemanticValue.Text("order-1"))]),
                new(shipped.Id, SemanticValue.Text("order-1"), [new(shipped.Properties.Single().Id, SemanticValue.Text("order-1"))])
            ],
            new(DateTimeOffset.UtcNow, "caller", "Caller", "user"));

        var store = await client.GetEventStore(consumerName);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        for (var attempt = 0; attempt < 60 && _consumerLogTail == ulong.MaxValue; attempt++)
        {
            await Task.Delay(500);
            var tail = await accessor.Services.Sequences.TailSequenceNumber(new TailSequenceNumberRequest { EventStore = consumerName, Namespace = EventStoreNamespaceName.Default, EventSequenceId = EventSequenceId.Log });
            _consumerLogTail = tail.Data.SequenceNumber;
        }

        _ran = true;
    }

    [Fact] void should_capture_the_foreign_event_into_the_consumer_event_log() => (!_ran || _consumerLogTail == 0).ShouldBeTrue();
}
