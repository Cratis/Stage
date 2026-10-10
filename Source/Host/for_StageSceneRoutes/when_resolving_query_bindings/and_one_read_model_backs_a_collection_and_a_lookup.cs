// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_resolving_query_bindings;

/// <summary>
/// The lookup's legacy route is the shortest route over the read model - the shape that once sent a list to it.
/// </summary>
public class and_one_read_model_backs_a_collection_and_a_lookup : given.registered_endpoints
{
    void Establish()
    {
        Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItems", "/api/workspaces/tracking/work-item-list/all-work-items", "/api/workspaces/tracking/all-work-items");
        Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.WorkItemById", "/api/workspaces/tracking/work-item-list/work-item-by-id", "/api/workspaces/tracking/work-item-by-id");
        Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItemSummaries", "/api/workspaces/tracking/work-item-list/all-work-item-summaries", "/api/workspaces/tracking/all-work-item-summaries");
    }

    void Because() => Resolve(
        Data("list", "AllWorkItems", "WorkItemSummary"),
        Data("lookup", "WorkItemById", "WorkItemSummary"),
        Table("table", "WorkItemSummary"));

    [Fact] void should_bind_the_list_to_its_collection_query() => Properties("list")["route"].ShouldEqual("/api/workspaces/tracking/all-work-items");
    [Fact] void should_bind_the_lookup_to_its_own_query() => Properties("lookup")["route"].ShouldEqual("/api/workspaces/tracking/work-item-by-id");
    [Fact] void should_bind_an_element_naming_only_the_read_model_to_its_conventional_collection() => Properties("table")["route"].ShouldEqual("/api/workspaces/tracking/all-work-item-summaries");
}
