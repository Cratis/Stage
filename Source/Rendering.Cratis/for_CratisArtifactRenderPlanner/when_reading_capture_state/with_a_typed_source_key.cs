// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_typed_source_key : given.a_translation_application
{
    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var capture = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate) with { Specifications = [] };
        _model = ExecutableSemanticModel.Create(LanguageVersion.V33, SemanticVersion.V33, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [capture] }] }]
        });
    }

    void Because() => Render();

    [Fact]
    void should_render_a_capture_only_concept_as_an_event_source_identity()
    {
        var identity = _assembly.GetTypes().Single(type => type.Name == "OrderId");
        identity.BaseType!.GetGenericTypeDefinition().ShouldEqual(typeof(EventSourceId<>));
        var reactor = _sources.Single(source => source.RelativePath.EndsWith("OrderIntake.cs", StringComparison.Ordinal)).Content;
        reactor.ShouldContain("EventForEventSourceId(@event.OrderId,");
        reactor.ShouldNotContain("@event.OrderId.ToString()");
    }

    [Fact]
    public async Task should_unwrap_the_uuid_key_and_preserve_the_typed_destination()
    {
        var store = Substitute.For<IEventStore>();
        var inbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(new EventSequenceId("inbox-storefront")).Returns(inbox);
        store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => call.Arg<Type>().GetEventType());
        inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>([]));
        var eventType = _assembly.GetTypes().Single(type => type.Name == "OrderPlaced");
        var keyType = eventType.GetConstructors().Single().GetParameters().Single().ParameterType;
        var id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var key = Activator.CreateInstance(keyType, id)!;
        var fact = Activator.CreateInstance(eventType, key)!;
        var context = EventContext.Empty with { SequenceNumber = 3, EventSourceId = id.ToString() };
        var reactor = Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == "OrderIntake"), store)!;
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnOrderPlaced")!.Invoke(reactor, [fact, context, ReactorDelivery.For(reactor, context)])!;
        effects.Cast<EventForEventSourceId>().Single().EventSourceId.Value.ShouldEqual(id.ToString());
        keyType.Name.ShouldEqual("OrderId");
        await inbox.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, context.EventSourceId, Arg.Any<IEnumerable<EventType>>());
    }
}
#endif
