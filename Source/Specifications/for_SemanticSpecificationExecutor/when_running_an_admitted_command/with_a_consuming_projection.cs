// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_consuming_projection : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(WithProjection(_specification), new([_specification.Id]), new());

    [Fact] void should_refuse_before_executing() => _report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_not_claim_success() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
}
