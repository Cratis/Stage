// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_observer_filters;

public class with_identity_and_filter : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Establish() => Prepare(FilteredSource().Replace("      reaction Worker", "      reaction Worker\n        runs as system", StringComparison.Ordinal));
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content;
    [Fact] void should_emit_identity_before_the_filter() => (_source.IndexOf("[ExecuteCommandsAsSystem", StringComparison.Ordinal) < _source.IndexOf("[FromEventSource", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_stable_reactor_identity() => _source.ShouldContain($"Reactor(\"{_located.Slice.Reactions.Single().Id}\")");
}
