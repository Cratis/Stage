// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class all_literal_projection
{
    // The all block carries a text literal, a concept literal and a non-null numeric literal.
    internal static readonly string Source = all_events_projection.Source + "\n" + """
            slice StateView AllLabelLookup
              readmodel AllLabelSummary
                projectId ProjectId
                label String
                title ProjectName
                score Decimal?
              query AllLabelById => AllLabelSummary?
                by projectId ProjectId
              projection AllLabelSummaryProjection => AllLabelSummary
                from ProjectRegistered key projectId
                all
                  label = "fixed"
                  title = "named"
                  score = 12.5
        """;
}
#endif
