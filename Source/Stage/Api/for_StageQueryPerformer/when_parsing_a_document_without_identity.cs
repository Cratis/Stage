// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_a_document_without_identity : given.a_stage_query_performer
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _performer.Parse(["""{"name":"Test item"}"""]));

    [Fact] void should_fail_instead_of_returning_an_unaddressable_result() => _error.ShouldBeOfExactType<InvalidStageReadModelDocument>();
}
