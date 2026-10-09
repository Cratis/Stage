// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_constructing_a_conventional_collection_query : Specification
{
    StageQueryPerformer _performer = null!;

    void Because() => _performer = new(typeof(DynamicReadModel), "b3b0d0a4-0e9b-4b23-9d2f-7d0f3b7a4f21", "AllReadModels", ["read-models"], false);

    [Fact] void should_declare_no_parameters() => _performer.Parameters.ShouldBeEmpty();
    [Fact] void should_keep_the_conventional_query_name() => _performer.Name.Value.ShouldEqual("AllReadModels");
}
