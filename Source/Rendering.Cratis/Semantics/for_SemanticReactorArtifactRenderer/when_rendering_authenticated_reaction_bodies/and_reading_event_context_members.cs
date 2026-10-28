// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_reading_event_context_members : a_rendered_reaction_body
{
    const string Body = "return context.EventSourceId.ToString() == \"\" || context.SequenceNumber.Value == 0 ? null : new object[] { Title };";

    void Because() => Render(reaction_body_model.Source(Body));

    [Fact] void should_publish_and_compile() => ShouldPublishAndCompile(Body);
}
#endif
