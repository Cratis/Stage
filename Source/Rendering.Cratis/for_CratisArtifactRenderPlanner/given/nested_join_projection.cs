// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class nested_join_projection
{
    // The nested object's reference is set by the from event; the join resolves the same event source.
    internal static readonly string Source = every_literal_join_projection.Source
        .Replace("module Projects\n", "type NestedLink\n  reference ProjectId\n  value ProjectName?\nmodule Projects\n", StringComparison.Ordinal) + "\n" + """
            slice StateView NestedJoinLookup
              readmodel NestedJoinSummary
                projectId ProjectId
                details NestedLink?
              query NestedJoinById => NestedJoinSummary?
                by projectId ProjectId
              projection NestedJoinSummaryProjection => NestedJoinSummary
                no automap
                from ProjectRegistered key projectId
                from ProjectRenamed
                nested details
                  from ProjectRegistered key projectId
                    reference = projectId
                    value = name
                  clear with ProjectRenamed
                  join linked on reference
                    with ProjectNamed
                      value = name
        """;
}
#endif
