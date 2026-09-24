// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_an_unsupported_identity : given.a_stage_query_performer
{
    [Theory]
    [InlineData("""{"id":null}""")]
    [InlineData("""{"id":[1,2]}""")]
    [InlineData("""{"id":{"part":[1]}}""")]
    [InlineData("[]")]
    public void should_fail_instead_of_returning_a_partial_result(string document) =>
        Catch.Exception(() => _performer.Parse([document])).ShouldBeOfExactType<InvalidStageReadModelDocument>();
}
