// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_cancellation_during_execution : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new() { Clock = new CancellingClock(cancellation) }, cancellation.Token);
    }

    [Fact] void should_report_cancelled_instead_of_failed() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Cancelled);

    sealed class CancellingClock(CancellationTokenSource cancellation) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            cancellation.Cancel();
            return DateTimeOffset.UnixEpoch;
        }
    }
}
