// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory.when_reporting_semantic_exclusions;

public class and_writing_without_inventory : given.an_excluded_request
{
    PlannedArtifact _report = null!;

    void Because()
    {
        _report = ArtifactRenderReports.CreateUnsupported(_excluded);
        _text = Text(_report);
    }

    [Fact] void should_write_the_managed_report_path() => _report.RelativePath.ShouldEqual("UNSUPPORTED.md");
    [Fact] void should_not_fabricate_counts() => _text.Contains("Owners:", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_fabricate_a_score() => _text.Contains("Score:", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_degraded_outcome() => _text.Contains("Outcome: Degraded.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_utf8_without_a_bom() => _report.Bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 }).ShouldBeFalse();
    [Fact] void should_still_require_inventory_for_completeness_json() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderReports.Create(_excluded, _context));
}
