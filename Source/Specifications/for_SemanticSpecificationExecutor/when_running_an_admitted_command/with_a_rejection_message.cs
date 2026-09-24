// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_rejection_message : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var rejected = _plan.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Commands.Length > 0).Specifications.Single(specification => specification.Name == "RejectingAnEmptyProjectName");
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([rejected.Id]), new());
    }

    [Fact] void should_pass() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_message() => _report.Results.Single().Trace!.Rejection.ShouldEqual("Project name is required");
}
