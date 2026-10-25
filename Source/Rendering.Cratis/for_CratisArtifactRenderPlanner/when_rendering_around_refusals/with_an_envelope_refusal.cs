// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_envelope_refusal : given.a_refused_specification
{
    void Establish() => _request = _request with { ImplementationRequirements = default };
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_partial_rendering() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_the_envelope_error() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    [Fact]
    void should_omit_refused_parts_and_compile_retained_artifacts()
    {
        var envelope = Assert.Single(_plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == _request.Model.Application.Id));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, envelope.Severity);
        Assert.Contains(_plan.ImplementationGaps, gap => gap.Kind == ArtifactImplementationGapKind.Rendering &&
            gap.Owner == envelope.Artifact.ToString() && gap.Hints.Contains(envelope.Message));
        Assert.Contains(_plan.Artifacts, artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        Assert.DoesNotContain(_plan.Artifacts, artifact => artifact.RelativePath != "STAGE-NOT-RUNNABLE.md" && artifact.Sources.Contains(_refused));
        var command = _request.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Commands.Length > 0).Commands.Single();
        global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Retained(_plan, command.Id);
#if DEBUG
        global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Compiles(_plan);
#endif
    }
    [Fact] void should_list_the_explicit_specification_refusal() => Assert.Contains(_plan.Refusals, refusal => refusal.Artifact == _refused);
    [Fact] void should_keep_the_explicit_specification_error() => Assert.Contains(_plan.Diagnostics, diagnostic =>
        diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _refused && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
}
