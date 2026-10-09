// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_list_queries;

public class with_a_keyed_list : given.a_list_query_runtime
{
    void Establish() => CreateProvider();
    async Task Because() => await Perform("ProjectsByName", new QueryArguments { ["name"] = "Shared" });

    [Fact] void should_expose_only_the_declared_argument() => _performer.Parameters.Select(parameter => parameter.Name).ShouldContainOnly("name");
    [Fact] void should_preserve_both_matching_instances() => ((object[])_result!).Cast<DynamicReadModel>().Select(model => model.Id).ShouldContainOnly("project-1", "project-2");
}
