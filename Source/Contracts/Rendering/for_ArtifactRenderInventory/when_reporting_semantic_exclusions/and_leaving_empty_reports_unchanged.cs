// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory.when_reporting_semantic_exclusions;

public class and_leaving_empty_reports_unchanged : given.an_excluded_request
{
    ArtifactRenderReports _actual = null!;
    ArtifactRenderReports _expected = null!;

    void Because()
    {
        var plan = ArtifactRenderPlan.Create(_request with { Exclusions = [] }, [], _plan.Diagnostics).WithInventory(_inventory);
        _actual = ArtifactRenderReports.Create(plan, _context);
        _expected = ArtifactRenderReports.Create(_plan, _context);
    }

    [Fact] void should_preserve_markdown_bytes() => _actual.Unsupported.Bytes.SequenceEqual(_expected.Unsupported.Bytes).ShouldBeTrue();
    [Fact] void should_preserve_completeness_bytes() => _actual.Completeness.Bytes.SequenceEqual(_expected.Completeness.Bytes).ShouldBeTrue();
    [Fact] void should_match_the_standalone_human_writer() => ArtifactRenderReports.CreateUnsupported(_plan).Bytes.SequenceEqual(_expected.Unsupported.Bytes).ShouldBeTrue();
}
