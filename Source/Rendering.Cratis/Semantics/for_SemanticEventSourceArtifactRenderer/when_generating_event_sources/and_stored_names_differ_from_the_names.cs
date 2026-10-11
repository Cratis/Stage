// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_stored_names_differ_from_the_names : a_routed_command
{
    string _definition = null!;

    void Establish() => _source = _source with { Name = "OrderItem", SourceKind = "Item", Streams = [_source.Streams[0] with { Name = "Primary", StreamKind = "Orders" }] };
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/OrderItemEventSource.cs"));
    }

    [Fact] void should_name_the_class_after_the_declaration() => _definition.Contains("class OrderItemEventSource", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_source_type() => _definition.Contains("EventSourceAttribute(\"Item\")", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_stream_type() => _definition.Contains("EventStreamAttribute(\"Orders\")", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_stream_on_the_command() => _code.Contains("OrderItemEventSource>(\"Orders\")", StringComparison.Ordinal).ShouldBeTrue();
}
