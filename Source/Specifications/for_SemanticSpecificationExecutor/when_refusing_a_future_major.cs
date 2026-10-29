// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_a_future_major : a_plan_at_an_esm_pair
{
    readonly List<SemanticSpecificationRunReport> _reports = [];

    async Task Because()
    {
        foreach (var (major, minor) in new (uint, uint)[] { (33, 1), (35, 0) })
        {
            var plan = WithApplication(application => application, major, minor);
            _reports.Add(await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new()));
        }
    }

    [Fact] void should_refuse_instead_of_executing() => _reports.ShouldEachConformTo(report => report.Results.Single().Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_stage_version_gate() => _reports.ShouldEachConformTo(report => report.Results.Single().Unsupported!.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
    [Fact] void should_name_the_range() => _reports.ShouldEachConformTo(report => report.Results.Single().Unsupported!.Details.Contains(">=32.0 <33", StringComparison.Ordinal));
}
