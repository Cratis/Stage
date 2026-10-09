// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_computing_the_digest : given.an_artifact_render_request
{
    ImmutableArray<PlannedArtifact> _artifacts;
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;
    string _digest = null!;
    void Establish()
    {
        _artifacts = [PlannedArtifact.CreateText("b.cs", "b", [_request.Model.Application.Id]), PlannedArtifact.CreateText("a.cs", "a")];
        _diagnostics = [new("B", ArtifactRenderDiagnosticSeverity.Warning, "b", default), new("A", ArtifactRenderDiagnosticSeverity.Information, "a", default)];
    }
    void Because() => _digest = Digest(_artifacts, _diagnostics);
    [Fact] void should_be_a_sha256_digest() => _digest.Length.ShouldEqual(64);
    [Fact] void should_ignore_enumeration_order() => Digest([.. _artifacts.Reverse()], [.. _diagnostics.Reverse()]).ShouldEqual(_digest);
    [Fact] void should_change_with_content() => Digest([_artifacts[0], PlannedArtifact.CreateText("a.cs", "changed")], _diagnostics).ShouldNotEqual(_digest);
    [Fact] void should_change_with_semantic_sources() => Digest([PlannedArtifact.CreateText("b.cs", "b"), _artifacts[1]], _diagnostics).ShouldNotEqual(_digest);
    [Fact] void should_change_with_diagnostics() => Digest(_artifacts, []).ShouldNotEqual(_digest);
    [Fact] void should_match_the_plan_computation() => ArtifactRenderPlan.Create(_request, _artifacts, _diagnostics).Digest.ShouldEqual(_digest);
    string Digest(ImmutableArray<PlannedArtifact> artifacts, ImmutableArray<ArtifactRenderDiagnostic> diagnostics) => ArtifactRenderPlan.ComputeDigest(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, _request.Model.Application.Name, artifacts, diagnostics);
}
