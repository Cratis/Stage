// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Matches stored route kinds without constraining stream identities.
/// </summary>
internal static class SemanticObserverFilterMatching
{
    internal static bool Matches(SemanticObserverFilter? filter, SemanticFixtureRoute? route, SemanticApplicationContext context)
    {
        if (filter is null) return true;
        if (route is null) return false;
        var source = context.Application.EventSources.Single(source => source.Id == filter.Source);
        var routedSource = context.Application.EventSources.Single(source => source.Id == route.Source);
        var routedStream = routedSource.Streams.Single(stream => stream.Id == route.Stream);

        return source.SourceKind == routedSource.SourceKind &&
            (filter.Stream is null || source.Streams.Single(stream => stream.Id == filter.Stream).StreamKind == routedStream.StreamKind);
    }
}
