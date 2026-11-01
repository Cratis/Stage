// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_applying_the_file_header;

public class with_an_unsafe_header_on_a_recreated_plan : given.an_artifact_render_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _again = null!;

    void Establish()
    {
        var input = ArtifactFileHeader.CreateInput(["unsafe \\u000a directive"]);
        _request = _request with { Profile = ArtifactRenderProfile.Create(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, [input]) };
        _first = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("Code.java", "code")], []);
    }

    void Because() => _again = ArtifactRenderPlan.Create(_request, _first.Artifacts, _first.Diagnostics);

    [Fact] void should_keep_the_refusal_without_artifacts() => _again.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_one_header_refusal() => _again.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-002");
    [Fact] void should_keep_the_diagnostics() => _again.Diagnostics.SequenceEqual(_first.Diagnostics).ShouldBeTrue();
    [Fact] void should_keep_the_digest() => _again.Digest.ShouldEqual(_first.Digest);

    [Theory]
    [InlineData(ArtifactRenderDiagnosticSeverity.Warning)]
    [InlineData(ArtifactRenderDiagnosticSeverity.Error)]
    void should_suppress_only_an_equivalent_header_diagnostic(ArtifactRenderDiagnosticSeverity severity)
    {
        var added = _first.Diagnostics.Single() with { Severity = severity, Message = "source.play: A different diagnostic." };
        var result = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("Code.java", "code")], [.. _first.Diagnostics.Select(diagnostic => diagnostic with { }), added, added], _first.ImplementationGaps, _first.Refusals, _first.RequiredRemovals);
        result.Artifacts.ShouldBeEmpty();
        result.Diagnostics.Count(diagnostic => diagnostic == _first.Diagnostics.Single()).ShouldEqual(1);
        result.Diagnostics.Count(diagnostic => diagnostic == added).ShouldEqual(2);
    }
}
