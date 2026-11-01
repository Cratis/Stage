// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_reading_an_unknown_event_member : a_rendered_reaction_body
{
    const string Body = "return @event.Missing is null ? null : new object[] { new TicketOpened(Title) };";

    void Because() => Render(reaction_body_model.Source(Body));

    [Fact] void should_refuse_with_cs1061() => ShouldRefuseWith("CS1061");
}
#endif
