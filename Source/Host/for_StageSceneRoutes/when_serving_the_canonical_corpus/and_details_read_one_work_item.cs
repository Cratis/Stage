// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_serving_the_canonical_corpus;

public class and_details_read_one_work_item : given.the_canonical_corpus
{
    [Fact] void should_bind_the_details_declaration_to_its_own_query() => Reading("WorkItemDetails", "GetWorkItem").Select(component => component.Properties["route"]).ShouldContainOnly(["/api/workspaces/tracking/get-work-item"]);
    [Fact] void should_leave_no_element_without_a_route() => Everywhere().Any(component => component.Properties.ContainsKey(StageSceneRoutes.RouteStatusProperty)).ShouldBeFalse();
    [Fact] void should_resolve_every_command_by_name() => _commandRoutes.Keys.ShouldContainOnly(["AddComment", "CloseWorkItem", "CreateWorkItem", "RenameWorkItem"]);
}
