// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_scoped_projection_specification : Specification
{
    string[] _errors = null!;
    string[] _reasons = null!;
    int _artifacts;

    void Because()
    {
        var source = when_rendering_scoped_projections.ScopedSource.Replace(
            "        then ProjectRegistered\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            "        then ProjectRegistered\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"\n        then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        _errors = [.. plan.Diagnostics.Select(_ => _.Code)];
        _reasons = [.. plan.Diagnostics.Select(_ => _.Message)];
        _artifacts = plan.Artifacts.Length;
    }

    [Fact] void should_reject_unproven_scoped_replay() => _errors.ShouldContain("STAGE-ESM-011");
    [Fact] void should_explain_why_a_flat_transition_is_not_equivalent() => _reasons.Any(_ => _.Contains("flat transition replay", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_partial_artifacts() => _artifacts.ShouldEqual(0);
}
