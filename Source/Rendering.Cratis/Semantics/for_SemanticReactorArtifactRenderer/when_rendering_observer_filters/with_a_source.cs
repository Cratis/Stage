// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_observer_filters;

public class with_a_source : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Establish() => Prepare(FilteredSource("Account"));
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content;
    [Fact] void should_emit_the_stored_source_filter() => _source.ShouldContain("[global::Cratis.Chronicle.Events.EventSourceTypeAttribute(\"stored-account\")]");
    [Fact] void should_not_route_productions_with_a_stream_attribute() => _source.ShouldNotContain("EventStreamTypeAttribute");
}
