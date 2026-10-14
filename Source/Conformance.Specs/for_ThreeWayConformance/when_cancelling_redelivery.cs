// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_cancelling_redelivery : a_recovery_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new(), cancellation.Token);
    }

    [Fact] void should_report_cancelled_rather_than_passed() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Cancelled);
    [Fact] void should_leave_the_report_incomplete() => _report.Completed.ShouldBeFalse();
}
#endif
