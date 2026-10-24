// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_renamed_capture_members : given.a_translation_specification_application
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";

    [Fact]
    public async Task should_compile_and_execute_a_generated_specification_calling_a_renamed_handler()
    {
        _model = Transform(_model, slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { Name = "OnShipmentDispatched" })] });
        Render(clientOnly: true);
        Assert.Empty(RenderedOutput.Warnings(_sources));
        await RunGenerated("when_receiving_a_shipment");
    }

    [Theory]
    [InlineData("Observe")]
    [InlineData("Previous")]
    [InlineData("Overlay")]
    [InlineData("SameState")]
    [InlineData("Same")]
    [InlineData("ConceptValue")]
    [InlineData("MissingCaptureField")]
    [InlineData("EmptyCaptureKey")]
    [InlineData("OnShipmentDispatched")]
    public async Task should_keep_arrival_suppression_key_isolation_and_replay_with_renamed_declarations(string name)
    {
        _model = Transform(_model, slice => slice with { Specifications = [], Captures = [.. slice.Captures.Select(capture => capture with { Name = name })] });
        Render();
        var store = Substitute.For<IEventStore>();
        var inbox = Substitute.For<IEventSequence>();
        var history = new List<AppendedEvent>();
        store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        store.GetEventSequence(new EventSequenceId("inbox-shipping")).Returns(inbox);
        inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. history]));
        var reactorType = Generated("TrackShipments", name);
        var sourceType = Generated("TrackShipments", "ShipmentDispatched");
        var method = reactorType.GetMethods().Single(candidate => candidate.GetParameters() is { Length: 2 } parameters && parameters[0].ParameterType == sourceType && parameters[1].ParameterType == typeof(EventContext));
        var effects = new List<EventForEventSourceId>();
        foreach (var key in new[] { First, First, Second })
        {
            var source = Activator.CreateInstance(sourceType, Activator.CreateInstance(_assembly.GetType("Contracts.Common.OrderId", true)!, Guid.Parse(key)))!;
            var position = (ulong)history.Count;
            history.Add(new(EventContext.Empty with { SequenceNumber = position, EventType = sourceType.GetEventType() }, source));
            var result = await (Task<IEnumerable<object>>)method.Invoke(Activator.CreateInstance(reactorType, store), [source, EventContext.Empty with { SequenceNumber = position }])!;
            effects.AddRange(result.Cast<EventForEventSourceId>());
        }
        Assert.Equal([First, Second], effects.Select(effect => effect.EventSourceId.Value));
        Assert.All(effects, effect => Assert.Equal(effect.EventSourceId.Value, effect.Event.GetType().GetProperty("OrderId")!.GetValue(effect.Event)!.ToString()));
        var replay = await (Task<IEnumerable<object>>)method.Invoke(Activator.CreateInstance(reactorType, store), [history[0].Content, EventContext.Empty with { SequenceNumber = 0UL }])!;
        Assert.Equal(First, Assert.Single(replay.Cast<EventForEventSourceId>()).EventSourceId.Value);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Changed")]
    [InlineData("Field")]
    [InlineData("Same")]
    [InlineData("Overlay")]
    [InlineData("Observe")]
    public async Task should_keep_added_transition_and_overlay_guards_in_actual_generated_specifications(string name)
    {
        _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.ToArray()), slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { Name = name })] });
        Render(clientOnly: true);
        Assert.Empty(RenderedOutput.Warnings(_sources));
        await RunGenerated("when_first_arrival");
        await RunGenerated("when_given_transition");
        await RunGenerated("when_overlay");
    }
}
#endif
