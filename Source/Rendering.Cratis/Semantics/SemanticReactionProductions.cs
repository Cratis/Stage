// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Enumerates reaction append sites across the application for identity admission.
/// </summary>
internal static class SemanticReactionProductions
{
    internal static IEnumerable<(SemanticReaction Reaction, SemanticProducedEvent Produced)> In(SemanticApplication application) =>
        SemanticVersionFeatures.Slices(application)
            .SelectMany(slice => slice.Reactions.IsDefault ? [] : slice.Reactions)
            .SelectMany(reaction => reaction.Triggers.SelectMany(trigger => trigger.Produces)
                .Select(produced => (reaction, produced)));
}
