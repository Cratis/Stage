// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_repeated_keyed_queries : a_command_only_plan
{
    [Fact]
    void should_reject_duplicate_query_keys_before_execution()
    {
        var specification = _specification with
        {
            ThenReadModels = _original.ThenReadModels,
            ThenQueries = [.. _original.ThenQueries, .. _original.ThenQueries]
        };

        Assert.Throws<InvalidSemanticContract>(() => WithProjection(specification));
    }
}
