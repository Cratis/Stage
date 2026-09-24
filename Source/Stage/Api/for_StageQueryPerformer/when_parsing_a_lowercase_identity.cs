// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_a_lowercase_identity : given.a_stage_query_performer
{
    DynamicReadModel _instance = null!;

    void Because() => _instance = (DynamicReadModel)Assert.Single(_performer.Parse(["""
        {"id":"8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1","name":"Test item","__sequenceNumber":1}
        """]));

    [Fact] void should_bind_the_identity() => _instance.Id.ShouldEqual("8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1");
    [Fact] void should_keep_modeled_properties() => _instance.Values["name"].GetString().ShouldEqual("Test item");
    [Fact] void should_not_duplicate_the_identity_as_an_extension_value() => _instance.Values.ContainsKey("id").ShouldBeFalse();
    [Fact] void should_remove_kernel_bookkeeping() => _instance.Values.ContainsKey("__sequenceNumber").ShouldBeFalse();
}
