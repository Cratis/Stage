// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_rendering_parent_comments : given.an_excluded_feature
{
    ArtifactRenderPlan _original = null!;

    void Because()
    {
        _plan = _planner.Plan(_request);
        _original = _planner.Plan(_request with { Exclusions = [] });
    }

    [Fact] void should_emit_a_feature_owned_comment() => Comment.Sources.Single().ShouldEqual(_feature.Id);
    [Fact] void should_place_the_file_in_the_feature() => Comment.RelativePath.ShouldEqual("Projects/Registration/ScreenplayExclusions.g.cs");
    [Fact] void should_name_the_unit_and_original_codes() => Text(Comment).ShouldEqual("// Differs from Screenplay: Projects/Registration/Broken excluded (PLAY0053)\n");
    [Fact] void should_report_without_an_identity_catalog() => Text(Report).Contains("Excluded from the executable model", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_fabricate_completeness_json() => _plan.Artifacts.Any(artifact => artifact.RelativePath == ".stage/completeness.json").ShouldBeFalse();
    [Fact] void should_keep_original_source_bytes() => _original.Artifacts.All(artifact => _plan.Artifacts.Single(current => current.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
    [Fact] void should_emit_nothing_for_an_empty_attachment() => _original.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("ScreenplayExclusions.g.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_warning_and_evidence_once_after_report_reconstruction() => _plan.PlanEvidence.Length.ShouldEqual(1);
}
