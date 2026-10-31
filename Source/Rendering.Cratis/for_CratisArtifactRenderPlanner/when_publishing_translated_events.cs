// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_publishing_translated_events : given.a_translation_application
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
    readonly List<AppendedEvent> _published = [];
    bool _refuse;
    Exception _refusal = null!;
    readonly IEventStore _store = Substitute.For<IEventStore>();
    readonly IEventSequence _outbox = Substitute.For<IEventSequence>();

    async Task Because()
    {
        Render();
        _store.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        _store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        _outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(call =>
            Task.FromResult<IImmutableList<AppendedEvent>>([.. _published.Where(fact => fact.Context.EventSourceId == call.Arg<EventSourceId>())]));
        _outbox.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(call =>
        {
            if (_refuse) return Task.FromResult(new AppendResult { Errors = [new AppendError("Refused")] });
            _published.Add(new(EventContext.EmptyWithEventSourceId(call.Arg<EventSourceId>()) with { SequenceNumber = new((ulong)_published.Count) }, call.Arg<object>()));
            return Task.FromResult(new AppendResult());
        });
        await Publish(First, "DHL");
        await Publish(First, "DHL");
        await Publish(Second, "DHL");
        await Publish(First, "UPS");
        await Publish(First, "DHL");
        _refuse = true;
        _refusal = await Assert.ThrowsAnyAsync<Exception>(() => Publish(First, "Other"));
        _refuse = false;
    }

    [Fact] void should_suppress_an_unchanged_public_record_after_reactivation() => _published.Count.ShouldEqual(4);
    [Fact] void should_keep_independent_event_source_keys() => _published.Select(fact => fact.Context.EventSourceId.Value).ShouldEqual([First, Second, First, First]);
    [Fact] void should_compare_to_the_last_fact_not_all_historical_facts() => _published.Select(fact => fact.Content.GetType().GetProperty("Carrier").GetValue(fact.Content)).ShouldEqual(["DHL", "DHL", "UPS", "DHL"]);
    [Fact] void should_publish_the_public_contract_not_the_private_trigger() => _published.ShouldEachConformTo(fact => fact.Content.GetType() == Generated("PublishOrderShipped", "OrderShipped"));
    [Fact]
    public async Task should_never_append_public_events_to_the_local_log()
    {
        await _store.EventLog.DidNotReceiveWithAnyArgs().Append(default!, default!);
    }

    [Fact]
    public void should_propagate_an_outbox_refusal()
    {
        _refusal.GetType().Name.ShouldEqual("PublicationAppendFailed");
        _refusal.Message.ShouldEqual("The public event could not be appended to the outbox.");
    }

    [Fact]
    public async Task should_compare_typed_datetime_values_instead_of_their_serialized_offset_text()
    {
        var instant = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        var published = await PublishCarriers(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime), instant, instant.ToOffset(TimeSpan.FromHours(-5)));
        published.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_compare_collection_contents_not_their_container_identity_or_concrete_type()
    {
        var published = await PublishCarriers(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true), new[] { "one", "two" }, new List<string> { "one", "two" });
        published.Count.ShouldEqual(1);
    }

    async Task<List<AppendedEvent>> PublishCarriers(SemanticTypeReference type, params object[] carriers)
    {
        var model = Transform(_model, slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with
            {
                Properties = [.. command.Properties.Select(property => property.Name == "carrier" ? property with { Type = type } : property)]
            })],
            Events = [.. slice.Events.Select(contract => contract with
            {
                Properties = [.. contract.Properties.Select(property => property.Name == "carrier" ? property with { Type = type } : property)]
            })]
        });
        var (_, _, assembly) = RenderModel(model);
        var published = new List<AppendedEvent>();
        var store = Substitute.For<IEventStore>();
        var outbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. published]));
        outbox.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(call =>
        {
            published.Add(new(EventContext.EmptyWithEventSourceId(call.Arg<EventSourceId>()) with { SequenceNumber = new((ulong)published.Count) }, call.Arg<object>()));
            return Task.FromResult(new AppendResult());
        });
        foreach (var carrier in carriers) await Publish(assembly, store, First, carrier);
        return published;
    }

    Task Publish(string key, object carrier) => Publish(_assembly, _store, key, carrier);

    static async Task Publish(Assembly assembly, IEventStore store, string key, object carrier)
    {
        var type = assembly.GetType("Contracts.Integration.Translation.PublishOrderShipped.OrderShippedPublisher", true);
        var reactor = Activator.CreateInstance(type, store);
        var source = Activator.CreateInstance(assembly.GetType("Contracts.Integration.Translation.PackOrder.OrderPacked", true), carrier);
        await (Task)type.GetMethod("OnOrderPacked").Invoke(reactor, [source, EventContext.EmptyWithEventSourceId(key)]);
    }
}
#endif
