// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_list_queries;

public class with_an_unkeyed_list : given.a_list_query_runtime
{
    void Establish() => CreateProvider();
    async Task Because() => await Perform("AllProjects", QueryArguments.Empty);

    [Fact] void should_expose_no_parameters() => _performer.Parameters.ShouldBeEmpty();
    [Fact] void should_preserve_every_instance() => ((object[])_result!).Cast<DynamicReadModel>().Select(model => model.Id).ShouldContainOnly("project-1", "project-2", "project-3");
}
