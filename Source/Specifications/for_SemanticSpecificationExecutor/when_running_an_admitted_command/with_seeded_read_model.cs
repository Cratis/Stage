// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_seeded_read_model : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var seeded = _specification with { GivenReadModels = _original.ThenReadModels };
        var plan = With(seeded);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([seeded.Id]), new());
    }

    [Fact] void should_block_seeded_read_models() => _report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.GivenReadModel);
}
