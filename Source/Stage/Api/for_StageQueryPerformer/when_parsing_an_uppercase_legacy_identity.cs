// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_an_uppercase_legacy_identity : given.a_stage_query_performer
{
    DynamicReadModel _instance = null!;

    void Because() => _instance = (DynamicReadModel)Assert.Single(_performer.Parse(["""{"Id":"legacy-source","name":"Test item"}"""]));

    [Fact] void should_keep_legacy_identity_readback() => _instance.Id.ShouldEqual("legacy-source");
    [Fact] void should_not_duplicate_the_legacy_identity() => _instance.Values.ContainsKey("Id").ShouldBeFalse();
}
