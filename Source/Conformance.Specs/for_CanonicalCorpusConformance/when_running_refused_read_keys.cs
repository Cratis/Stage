// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_running_refused_read_keys : Specification
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public async Task should_execute_all_six_source_authored_decision_0091_vectors(string formName)
    {
        // The pinned reference predates decision 0091. Assert its published expectations directly,
        // rather than treating its acceptance of a stored '*' instance as the expected outcome.
        var plan = refused_read_keys.Compile(formName);
        var selected = plan.Specifications.Values.Where(specification => refused_read_keys.Names.Contains(specification.Name, StringComparer.Ordinal)).ToArray();
        selected.Length.ShouldEqual(6);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([.. selected.Select(specification => specification.Id)]), new());
        report.Results.Count.ShouldEqual(6);
        foreach (var result in report.Results)
        {
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{formName}/{result.Name}: {result.Outcome} {string.Join(';', result.Failures)}");
            result.ExecutionKind.ShouldEqual("Rejected");
            result.Trace!.Facts.ShouldBeEmpty();
        }
    }
}
#endif
