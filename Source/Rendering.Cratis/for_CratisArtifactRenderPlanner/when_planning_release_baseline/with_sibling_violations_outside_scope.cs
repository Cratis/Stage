// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_sibling_violations_outside_scope : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(_assessment) with { Scope = new(ArtifactRenderScopeKind.Slice, _projectLookup.Id) });
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_sibling_changes_and_missing_revisions() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-001", "STAGE-BASELINE-002");
    [Fact] void should_not_emit_the_sibling_command() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeFalse();
}
#endif
