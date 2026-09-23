// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_an_empty_identity : given.a_stage_query_performer
{
    DynamicReadModel _instance = null!;

    void Because() => _instance = (DynamicReadModel)Assert.Single(_performer.Parse(["""{"id":""}"""]));

    [Fact] void should_preserve_the_valid_empty_key() => _instance.Id.ShouldEqual(string.Empty);
}
