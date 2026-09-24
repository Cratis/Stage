// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_multiple_specs_cancelled : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    int _expectedCount;

    async Task Because()
    {
        _expectedCount = _plan.Specifications.Count;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _report = await new SemanticSpecificationExecutor().Run(_plan, SemanticSpecificationSelection.All, new(), cancellation.Token);
    }

    [Fact] void should_include_every_unrun_specification() => _report.Results.Count.ShouldEqual(_expectedCount);
    [Fact] void should_mark_every_unrun_specification_cancelled() => Xunit.Assert.All(_report.Results, record => record.Outcome.ShouldEqual(SemanticSpecificationOutcome.Cancelled));
}
