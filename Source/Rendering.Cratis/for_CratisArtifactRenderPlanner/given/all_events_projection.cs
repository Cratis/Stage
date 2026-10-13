// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class all_events_projection
{
    internal static readonly string Source = every_literal_projection.Source + "\n" + """
            slice StateView AllLookup
              readmodel AllSummary
                projectId ProjectId
                lastSeen ProjectId?
              query AllById => AllSummary?
                by projectId ProjectId
              projection AllSummaryProjection => AllSummary
                from ProjectRegistered key projectId
                all
                  lastSeen = $eventSourceId
        """;
}
#endif
