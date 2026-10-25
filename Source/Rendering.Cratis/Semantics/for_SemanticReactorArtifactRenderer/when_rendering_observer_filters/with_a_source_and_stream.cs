// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_observer_filters;

public class with_a_source_and_stream : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content;
    [Fact] void should_emit_the_typed_filter_with_the_stored_stream() => _source.ShouldContain("[FromEventSource<AccountEventSource>(AccountEventSource.Notes)]");
    [Fact] void should_not_route_productions_with_a_stream_attribute() => _source.ShouldNotContain("[EventStreamType");
}
