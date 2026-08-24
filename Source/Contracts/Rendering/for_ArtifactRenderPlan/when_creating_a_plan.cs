// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_creating_a_plan : given.an_artifact_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _plan = ArtifactRenderPlan.Create(
            _request,
            [
                PlannedArtifact.CreateText("z\\Project.cs", "first\r\nsecond\r"),
                PlannedArtifact.CreateBinary("a/logo.bin", [2, 1])
            ],
            [
                new("CRATIS0002", ArtifactRenderDiagnosticSeverity.Warning, "Second", default),
                new("CRATIS0001", ArtifactRenderDiagnosticSeverity.Information, "First", default)
            ]);
    }

    [Fact] void should_be_successful_without_errors() => _plan.Success.ShouldBeTrue();
    [Fact] void should_carry_the_application_name() => _plan.ApplicationName.ShouldEqual("Projects");
    [Fact] void should_carry_the_semantic_revision() => _plan.SemanticRevision.ShouldEqual(_request.Model.Revision);
    [Fact] void should_order_artifacts_by_normalized_path() => _plan.Artifacts.Select(_ => _.RelativePath).SequenceEqual(["a/logo.bin", "z/Project.cs"]).ShouldBeTrue();
    [Fact] void should_normalize_text_to_utf8_lf() => Encoding.UTF8.GetString(_plan.Artifacts.Single(_ => _.Kind == PlannedArtifactKind.Text).Bytes.AsSpan()).ShouldEqual("first\nsecond\n");
    [Fact] void should_hash_every_artifact() => _plan.Artifacts.All(_ => _.Sha256.Length == 64).ShouldBeTrue();
    [Fact] void should_order_diagnostics_deterministically() => _plan.Diagnostics.Select(_ => _.Code).SequenceEqual(["CRATIS0001", "CRATIS0002"]).ShouldBeTrue();
}
