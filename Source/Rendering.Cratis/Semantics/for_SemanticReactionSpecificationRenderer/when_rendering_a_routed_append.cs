// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_a_routed_append : given.a_reaction_specification
{
    internal const string RoutedSource = """
        eventsource WorkSource
          id "Work"
          identifier String
          stream Notes
            id "Notes"
            streamId String
        """;

    void Establish()
    {
        Prepare(RoutedSource + "\n" + ProductionSource);
        var source = _context.Application.EventSources.Single();
        _specification = _specification with
        {
            WhenAppended = _specification.WhenAppended! with
            {
                EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("one")),
                Route = new(source.Id, source.Streams.Single().Id) { StreamId = SemanticValue.Text("notes") }
            }
        };
    }
    void Because() => Render();
    [Fact] void should_supply_the_modeled_stream_to_the_reactor() => _source.ShouldContain(".OnStream(eventSourceType: \"Work\", eventStreamType: global::Reactions.EventSources.WorkSourceEventSource.Notes, eventStreamId: \"notes\")");
    [Fact] void should_keep_the_production_unrouted() => _source.ShouldContain("EventStreamType.ShouldEqual(global::Cratis.Chronicle.Events.EventStreamType.All)");
}
