// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedSpecificationResults;

public class when_reading_the_native_summary
{
    [Fact] void should_accept_a_complete_passing_run() => "Total: 12, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1,527s".ShouldHavePassedSpecifications(12);
    [Fact] void should_accept_the_expected_failure_count() => "Total: 6, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0".ShouldHaveFailedSpecifications(1);

    [Theory]
    [InlineData("Passed!")]
    [InlineData("Total: 0, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0")]
    [InlineData("Total: 1, Errors: 1, Failed: 0, Skipped: 0, Not Run: 0")]
    [InlineData("Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0")]
    [InlineData("Total: 1, Errors: 0, Failed: 0, Skipped: 1, Not Run: 0")]
    [InlineData("Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 1")]
    [InlineData("Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0\nTotal: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0")]
    public void should_reject_missing_empty_failed_skipped_or_ambiguous_runs(string output) => Assert.NotNull(Record.Exception(() => output.ShouldHavePassedSpecifications()));

    [Fact]
    void should_reject_an_unexpected_discovery_count() => Assert.NotNull(Record.Exception(() => "Total: 11, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0".ShouldHavePassedSpecifications(12)));

    [Fact]
    void should_not_count_runner_errors_as_expected_failures() => Assert.NotNull(Record.Exception(() => "Total: 1, Errors: 1, Failed: 0, Skipped: 0, Not Run: 0".ShouldHaveFailedSpecifications(1)));
}
#endif
