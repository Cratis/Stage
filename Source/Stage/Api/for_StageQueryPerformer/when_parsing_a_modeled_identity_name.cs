// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_a_modeled_identity_name : given.a_stage_query_performer
{
    DynamicReadModel _instance = null!;

    void Because() => _instance = (DynamicReadModel)Assert.Single(_performer.Parse(["""
        {"id":"source-1","Id":"modeled id","ID":"model uppercase id"}
        """]));

    [Fact] void should_bind_only_the_exact_kernel_identity() => _instance.Id.ShouldEqual("source-1");
    [Fact] void should_keep_the_modeled_id_property() => _instance.Values["Id"].GetString().ShouldEqual("modeled id");
    [Fact] void should_keep_the_modeled_uppercase_property() => _instance.Values["ID"].GetString().ShouldEqual("model uppercase id");
}
