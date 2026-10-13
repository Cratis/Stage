// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_observer_filters;

public class without_a_filter : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content;
    [Fact] void should_not_emit_source_metadata() => _source.ShouldNotContain("EventSourceTypeAttribute");
    [Fact] void should_not_emit_a_typed_filter() => _source.ShouldNotContain("FromEventSourceAttribute");
}
