// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider;

public class when_omitting_an_optional_filter : given.a_list_query_runtime
{
    void Establish() => CreateProvider();
    async Task Because() => await Perform("FilteredProjects", new QueryArguments { ["name"] = "Shared" });

    [Fact] void should_not_narrow_on_the_omitted_filter() => ((object[])_result!).Cast<DynamicReadModel>().Select(model => model.Id).ShouldContainOnly("project-1", "project-2");
}
