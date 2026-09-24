// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_keyed_query : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var plan = With(_specification with { ThenQueries = _original.ThenQueries });
        _report = await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new());
    }

    [Fact] void should_block_query_execution() => _report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Query);
}
