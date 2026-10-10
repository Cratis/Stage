// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// The backend the screen-composition corpus renders, at the two places its Debug build failed before the corpus was
/// built end to end: an enum concept value, and the result field of a generated query specification.
/// </summary>
public class when_rendering_the_screen_composition_backend_specifications : a_screen_composition_render
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan();

    [Fact] void should_render_an_enum_concept_value_as_its_member() => Text(_plan, "Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs").ShouldContain("global::Workspaces.Common.WorkStatus.Closed");
    [Fact] void should_not_construct_an_enum_concept() => Text(_plan, "Workspaces/Tracking/CloseWorkItem/CloseWorkItem.cs").ShouldNotContain("new global::Workspaces.Common.WorkStatus(");
    [Fact] void should_declare_only_the_collection_result_for_a_list_query() => Text(_plan, ListSpecification).ShouldNotContain("? _result;");
    [Fact] void should_declare_the_collection_result_for_a_list_query() => Text(_plan, ListSpecification).ShouldContain("_results = [];");
    [Fact] void should_declare_only_the_single_result_for_a_single_query() => Text(_plan, SingleSpecification).ShouldNotContain("_results");
    [Fact] void should_declare_the_single_result_for_a_single_query() => Text(_plan, SingleSpecification).ShouldContain("? _result;");

    const string ListSpecification = "Workspaces/Tracking/WorkItemComments/when_showing_comments_for_awork_item_is_queried.cs";
    const string SingleSpecification = "Workspaces/Tracking/WorkItemDetails/when_showing_renamed_details_is_queried.cs";
}
