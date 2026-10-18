// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_a_partial_plan;

public class with_refused_specifications : given.an_artifact_render_request
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _withoutRefusals = null!;

    void Because()
    {
        var artifacts = new[] { PlannedArtifact.CreateText("app.cs", "application") };
        var diagnostics = new[] { new ArtifactRenderDiagnostic("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, "Unsupported specification", _request.Model.Application.Id) };
        _plan = ArtifactRenderPlan.Create(_request, [.. artifacts], [.. diagnostics], [], [new(_request.Model.Application.Id, "specification", ["STAGE-ESM-024", "STAGE-ESM-011", "STAGE-ESM-011"])]);
        _withoutRefusals = ArtifactRenderPlan.Create(_request, [.. artifacts], [.. diagnostics]);
    }

    [Fact] void should_distinguish_partial_output() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_success_false() => _plan.Success.ShouldBeFalse();
    [Fact] void should_keep_the_original_error() => _plan.Diagnostics.SequenceEqual(_withoutRefusals.Diagnostics).ShouldBeTrue();
    [Fact] void should_sort_and_deduplicate_codes() => _plan.Refusals.Single().Codes.SequenceEqual(["STAGE-ESM-011", "STAGE-ESM-024"]).ShouldBeTrue();
    [Fact] void should_hash_the_refusals() => _plan.Digest.ShouldNotEqual(_withoutRefusals.Digest);
    [Fact] void should_preserve_the_original_digest_for_empty_refusals() => ArtifactRenderPlan.Create(_request, _withoutRefusals.Artifacts, _withoutRefusals.Diagnostics, [], []).Digest.ShouldEqual(_withoutRefusals.Digest);
    [Fact] void should_distinguish_a_refused_plan() => _withoutRefusals.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_distinguish_a_complete_plan() => ArtifactRenderPlan.Create(_request, _plan.Artifacts, []).Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
}
