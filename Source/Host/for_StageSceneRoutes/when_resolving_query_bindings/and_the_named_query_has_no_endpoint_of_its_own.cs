// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_resolving_query_bindings;

/// <summary>
/// The host serves no endpoint under the bound query's own name, only the read model's conventional queries. An
/// unkeyed collection binding reads the conventional collection - the same rows - while a keyed or single-result
/// binding is never widened to it.
/// </summary>
public class and_the_named_query_has_no_endpoint_of_its_own : given.registered_endpoints
{
    void Establish()
    {
        Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.WorkItemById", "/api/workspaces/tracking/work-item-list/work-item-by-id", "/api/workspaces/tracking/work-item-by-id");
        Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItemSummaries", "/api/workspaces/tracking/work-item-list/all-work-item-summaries", "/api/workspaces/tracking/all-work-item-summaries");
        Query("Workspaces.Tracking.Comments.CommentView.AllCommentViews", "/api/workspaces/tracking/comments/all-comment-views", "/api/workspaces/tracking/all-comment-views");
    }

    void Because() => Resolve(
        Binding("list", "AllWorkItems", "WorkItemSummary", isCollection: true, by: null),
        Binding("thread", "CommentsForWorkItem", "CommentView", isCollection: true, by: "workItemId"),
        Binding("single", "CurrentWorkItem", "WorkItemSummary", isCollection: false, by: null));

    [Fact] void should_read_the_conventional_collection_for_an_unkeyed_collection_binding() => Properties("list")["route"].ShouldEqual("/api/workspaces/tracking/all-work-item-summaries");
    [Fact] void should_not_widen_a_keyed_binding_to_the_unfiltered_collection() => Properties("thread").ContainsKey("route").ShouldBeFalse();
    [Fact] void should_report_the_keyed_binding_unresolved() => Properties("thread")[StageSceneRoutes.RouteStatusProperty].ShouldEqual("unresolved");
    [Fact] void should_not_resolve_a_single_result_binding_to_a_collection() => Properties("single").ContainsKey("route").ShouldBeFalse();
}
