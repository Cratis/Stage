// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_an_invalid_document : given.a_stage_query_performer
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _performer.Parse(["""
        {"id":"8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"}
        """, "{invalid"]));

    [Fact] void should_fail_instead_of_returning_a_partial_result() => _error.ShouldBeOfExactType<InvalidStageReadModelDocument>();
}
