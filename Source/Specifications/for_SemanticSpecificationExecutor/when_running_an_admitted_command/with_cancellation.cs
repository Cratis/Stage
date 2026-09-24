// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_cancellation : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new(), cancellation.Token);
    }

    [Fact] void should_report_cancelled() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Cancelled);
    [Fact] void should_not_mark_report_complete() => _report.Completed.ShouldBeFalse();
}
