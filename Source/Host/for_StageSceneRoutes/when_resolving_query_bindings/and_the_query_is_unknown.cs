// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_resolving_query_bindings;

public class and_the_query_is_unknown : given.registered_endpoints
{
    void Establish() => Query("Workspaces.Tracking.WorkItemList.WorkItemSummary.WorkItemById", "/api/workspaces/tracking/work-item-list/work-item-by-id", "/api/workspaces/tracking/work-item-by-id");

    void Because() => Resolve(Data("list", "AllWorkItems", "WorkItemSummary"));

    [Fact] void should_not_fall_back_to_another_query_over_the_read_model() => Properties("list").ContainsKey("route").ShouldBeFalse();
    [Fact] void should_mark_the_binding_unresolved() => Properties("list")[StageSceneRoutes.RouteStatusProperty].ShouldEqual("unresolved");
    [Fact] void should_say_which_query_was_not_found() => ((string)Properties("list")[StageSceneRoutes.RouteDiagnosticProperty]!).ShouldContain("Query 'AllWorkItems' over 'WorkItemSummary'");
}
