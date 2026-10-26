// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class every_literal_join_projection
{
    internal const string Company = "4fa85f64-5717-4562-b3fc-2c963f66afa7";

    // Company is not the identifier. Its original from mapping declares the backfill dependency,
    // while every replaces its value before Chronicle resolves the joined event.
    internal static readonly string Source = all_literal_projection.Source + "\n" + $$"""
            slice StateView CompanyLookup
              readmodel CompanySummary
                projectId ProjectId
                company ProjectId
                joined ProjectName?
                label String
                score Decimal?
              query CompanyById => CompanySummary?
                by projectId ProjectId
              projection CompanySummaryProjection => CompanySummary
                no automap
                from ProjectRegistered key projectId
                  company = projectId
                join companyDetails on company
                  with ProjectNamed
                    joined = name
                every
                  exclude children
                  company = "{{Company}}"
                  label = "fixed"
                  score = 12.5
                remove with ProjectRemoved
        """;
}
#endif
