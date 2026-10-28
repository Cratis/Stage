// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_applying_the_file_header;

public class with_a_malformed_header_on_a_recreated_plan : given.an_artifact_render_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _again = null!;

    void Establish()
    {
        var input = ArtifactRenderInput.Create(ArtifactFileHeader.InputName, "2", ArtifactFileHeader.CreateInput(["hello"]).Bytes);
        _request = _request with { Profile = ArtifactRenderProfile.Create(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, [input]) };
        _first = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("Code.cs", "code")], []);
    }

    void Because() => _again = ArtifactRenderPlan.Create(_request, _first.Artifacts, [.. _first.Diagnostics.Select(diagnostic => diagnostic with { })]);

    [Fact] void should_keep_one_header_refusal() => _again.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
    [Fact] void should_keep_the_diagnostics() => _again.Diagnostics.SequenceEqual(_first.Diagnostics).ShouldBeTrue();
    [Fact] void should_keep_the_digest() => _again.Digest.ShouldEqual(_first.Digest);

    [Theory]
    [InlineData(ArtifactRenderDiagnosticSeverity.Warning)]
    [InlineData(ArtifactRenderDiagnosticSeverity.Error)]
    void should_preserve_added_diagnostics_during_reconstruction(ArtifactRenderDiagnosticSeverity severity)
    {
        var added = new ArtifactRenderDiagnostic("SOURCE-001", severity, "source.play: A source diagnostic.", _request.Model.Application.Id);
        var result = ArtifactRenderPlan.Create(_request, _first.Artifacts, [.. _first.Diagnostics, added, added], _first.ImplementationGaps, _first.Refusals, _first.RequiredRemovals);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-HEADER-001").ShouldEqual(1);
        result.Diagnostics.Count(diagnostic => diagnostic == added).ShouldEqual(2);
    }
}
