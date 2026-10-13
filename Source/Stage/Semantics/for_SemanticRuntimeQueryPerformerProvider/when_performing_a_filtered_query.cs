// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider;

public class when_performing_a_filtered_query : given.a_list_query_runtime
{
    void Establish() => CreateProvider();
    async Task Because() => await Perform("FilteredProjects", new QueryArguments { ["name"] = "Shared", ["projectId"] = "project-2" });

    [Fact] void should_combine_all_supplied_filters() => ((object[])_result!).Cast<DynamicReadModel>().Select(model => model.Id).ShouldContainOnly("project-2");
    [Fact] void should_expose_both_filter_parameters() => _performer.Parameters.Select(parameter => parameter.Name).ShouldContainOnly("name", "projectId");
}
