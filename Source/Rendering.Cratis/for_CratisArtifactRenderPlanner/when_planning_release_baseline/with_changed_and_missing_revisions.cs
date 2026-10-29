// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_changed_and_missing_revisions : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(_assessment));
    [Fact] void should_succeed_with_warnings_only() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_every_affected_revision() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-001", "STAGE-BASELINE-002");
    [Fact] void should_report_warning_severity() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().All(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_preserve_artifact_paths() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldEqual(_unchecked.Artifacts.Select(artifact => artifact.RelativePath));
    [Fact] void should_preserve_every_artifact_byte() => _plan.Artifacts.Zip(_unchecked.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_include_the_first_differing_path() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-BASELINE-001").Message.ShouldContain("$.properties[0].type.primitive");
}
#endif
