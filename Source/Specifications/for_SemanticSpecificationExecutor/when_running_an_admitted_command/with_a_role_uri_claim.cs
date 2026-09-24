// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_role_uri_claim : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var specification = _specification with { GivenCaller = new SemanticCaller(true, [], [new(ClaimTypes.Role, "Registrar")]) };
        var plan = With(specification);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_block_ambiguous_claim_type_before_execution() => Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Unsupported && _report.Results.Single().Unsupported?.Capability == StageExecutionCapability.Authorization);
}
