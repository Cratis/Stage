// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_filtering_by_an_event_source : a_rendered_reaction_body
{
    const string Body = "return null;";

    void Because() => Render(a_filtered_reaction.Definitions + "\n" + reaction_body_model.Source(Body)
        .Replace("      reaction Notifier", "      reaction Notifier\n        from Account.Notes", StringComparison.Ordinal));

    [Fact] void should_publish_and_compile() => ShouldPublishAndCompile(Body);
}
#endif
