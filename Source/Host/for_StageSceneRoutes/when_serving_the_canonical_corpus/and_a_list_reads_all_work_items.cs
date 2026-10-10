// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_serving_the_canonical_corpus;

public class and_a_list_reads_all_work_items : given.the_canonical_corpus
{
    (int Status, string Body, string? EndpointName) _response;

    async Task Because() => _response = await Request("GET", _queryRoutes["AllWorkItems"]);

    [Fact] void should_resolve_the_query_to_its_own_route() => _queryRoutes["AllWorkItems"].ShouldEqual("/api/workspaces/tracking/all-work-items");
    [Fact] void should_bind_every_list_declaration_to_that_route() => Reading("WorkItemList", "AllWorkItems").Select(component => component.Properties["route"]).ShouldContainOnly(["/api/workspaces/tracking/all-work-items"]);
    [Fact] void should_never_bind_the_list_to_the_single_item_lookup() => Reading("WorkItemList", "AllWorkItems").Any(component => (component.Properties["route"] as string)!.Contains("by-id", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_succeed() => _response.Status.ShouldEqual(200);
    [Fact] void should_return_both_work_items() => Values(_response, "title").ShouldContainOnly([TitleA, TitleB]);
}
