// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_reducer_observer_filters;

public class with_observer_filter_matching : given.a_filtered_reducer
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void should_distinguish_unfiltered_and_unrouted(bool routed, bool filtered, bool matches)
    {
        var plan = Plan("seed", "none", true);
        var source = plan.Model.Application.EventSources.Single(source => source.Name == "Account");
        var route = routed ? new SemanticEventRoute(source.SourceKind, source.Streams.Single(stream => stream.Name == "Notes").StreamKind, null) : null;
        SemanticObserverFilters.Matches(plan.Model.Application, filtered ? plan.Model.Application.Modules.Single().Features.Single().Slices.Single().Reducers.Single().From : null, route).ShouldEqual(matches);
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, "raw/id", true)]
    [InlineData(true, "raw/id", false)]
    public void should_match_stored_kinds_and_not_raw_stream_ids(bool streamFilter, string? streamId, bool matches)
    {
        var plan = Plan("seed", "none", true);
        var source = plan.Model.Application.EventSources.Single(source => source.Name == "Account");
        var route = new SemanticEventRoute(source.SourceKind, source.Streams.Single(stream => stream.Name == "Archive").StreamKind, streamId);
        var filter = streamFilter ? plan.Model.Application.Modules.Single().Features.Single().Slices.Single().Reducers.Single().From : new SemanticObserverFilter(source.Id, null);
        SemanticObserverFilters.Matches(plan.Model.Application, filter, route).ShouldEqual(matches);
    }
}
