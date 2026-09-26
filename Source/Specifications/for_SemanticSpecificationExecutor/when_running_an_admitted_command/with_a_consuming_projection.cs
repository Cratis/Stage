// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_consuming_projection : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var specification = _specification with { ThenReadModels = _original.ThenReadModels, ThenQueries = _original.ThenQueries };
        var plan = WithProjection(specification);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_match_the_reference() => _report.Results.Single().Outcome.ShouldEqual(_reference.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed);
    [Fact] void should_execute_the_projection() => Assert.NotEmpty(_report.Results.Single().Trace!.ReadModels);
}
