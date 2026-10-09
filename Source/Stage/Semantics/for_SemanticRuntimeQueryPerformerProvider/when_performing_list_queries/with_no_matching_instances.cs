// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_list_queries;

public class with_no_matching_instances : given.a_list_query_runtime
{
    void Establish() => CreateProvider();
    async Task Because() => await Perform("ProjectsByName", new QueryArguments { ["name"] = "Missing" });

    [Fact] void should_preserve_an_empty_collection_instead_of_a_null_instance() => ((object[])_result!).ShouldBeEmpty();
}
