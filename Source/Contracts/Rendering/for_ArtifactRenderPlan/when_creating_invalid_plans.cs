// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_creating_invalid_plans : given.an_artifact_render_request
{
    Exception _caseCollision = null!;
    Exception _duplicate = null!;
    Exception _invalidScope = null!;
    Exception _traversal = null!;
    ArtifactRenderPlan _withError = null!;

    void Because()
    {
        _duplicate = Catch.Exception(() => ArtifactRenderPlan.Create(
            _request,
            [PlannedArtifact.CreateText("Project.cs", "one"), PlannedArtifact.CreateText("Project.cs", "two")],
            []));
        _caseCollision = Catch.Exception(() => ArtifactRenderPlan.Create(
            _request,
            [PlannedArtifact.CreateText("Project.cs", "one"), PlannedArtifact.CreateText("project.cs", "two")],
            []));
        _traversal = Catch.Exception(() => PlannedArtifact.CreateText("../Project.cs", "content"));
        _invalidScope = Catch.Exception(() => ArtifactRenderPlan.Create(
            _request with { Scope = new(ArtifactRenderScopeKind.Slice, SemanticId.Parse($"sem1:{new string('0', 64)}")) },
            [],
            []));
        _withError = ArtifactRenderPlan.Create(
            _request,
            [],
            [new("CRATIS0001", ArtifactRenderDiagnosticSeverity.Error, "Cannot render", default)]);
    }

    [Fact] void should_reject_duplicate_paths() => _duplicate.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_reject_case_collisions() => _caseCollision.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_reject_traversal() => _traversal.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_reject_a_scope_outside_the_model() => _invalidScope.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_make_error_diagnostics_non_publishable() => _withError.Success.ShouldBeFalse();
}
