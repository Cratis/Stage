// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Matches observer filters against stored route kinds, independently of the stream id.
/// </summary>
public static class SemanticObserverFilters
{
    /// <summary>
    /// Tests a concrete route using the Screenplay observer-filter semantics.
    /// </summary>
    /// <param name="application">The application's route declarations.</param>
    /// <param name="filter">The observer's optional source and stream filter.</param>
    /// <param name="route">The formatted fact route, or null for an unrouted fact.</param>
    /// <returns>Whether the observer consumes the fact's route.</returns>
    public static bool Matches(SemanticApplication application, SemanticObserverFilter? filter, SemanticEventRoute? route)
    {
        if (filter is null) return true;
        if (route is null) return false;
        var source = application.EventSources.Single(source => source.Id == filter.Source);
        return source.SourceKind == route.SourceKind &&
            (filter.Stream is null || source.Streams.Single(stream => stream.Id == filter.Stream).StreamKind == route.StreamKind);
    }

    // Admission only needs stored kinds; execution formats and validates stream ids before calling Matches.
    internal static SemanticEventRoute? Kinds(SemanticExecutionPlan plan, SemanticFixtureRoute? fixture)
    {
        if (fixture is null) return null;
        var source = plan.Model.Application.EventSources.Single(source => source.Id == fixture.Source);
        return new(source.SourceKind, source.Streams.Single(stream => stream.Id == fixture.Stream).StreamKind, null);
    }
}
