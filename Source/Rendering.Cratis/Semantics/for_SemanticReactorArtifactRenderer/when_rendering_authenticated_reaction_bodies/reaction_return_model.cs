// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

internal static class reaction_return_model
{
    internal static LoadedSemanticModel Load(string body, bool file = false) => reaction_body_model.LoadSource(
        reaction_body_model.Source(body, file) + "\n" + """
                  command Notify
                    id String identifier
                    title String
                    produces Notified
                      for id
                      title = title
                  event Notified
                    title String
            """,
        file ? body : null);
}
#endif
