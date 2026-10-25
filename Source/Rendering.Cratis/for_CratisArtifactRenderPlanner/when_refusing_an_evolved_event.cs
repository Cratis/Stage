// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_evolved_event : a_v4_reducer
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_retain_the_v4_model() => _request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_refuse_without_migration_rendering() => rendering_refusals.Warning(_plan, "STAGE-ESM-026", _event.Id).Artifact.ShouldEqual(_event.Id);
    [Fact] void should_also_report_transition_analysis() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-023" && diagnostic.Artifact == _request.Model.Application.Id).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_emit_no_event_or_reducer_artifacts()
    {
        rendering_refusals.Omitted(_plan, _event.Id);
        Assert.DoesNotContain(_plan.Artifacts, artifact => artifact.RelativePath.EndsWith("Reducer.cs", StringComparison.Ordinal));
    }
#if DEBUG
    [Fact] void should_compile_the_retained_output() => rendering_refusals.Compiles(_plan);
#endif
    [Fact] void should_refuse_the_reducer_dependency_outside_the_selected_slice()
    {
        var context = new SemanticApplicationContext(_request, new("Projects", "Projects"));
        var reducer = context.SelectedSlices().Single(located => !located.Slice.Reducers.IsEmpty);
        var request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, reducer.Slice.Id) };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        rendering_refusals.Warning(plan, "STAGE-ESM-026", _event.Id).Artifact.ShouldEqual(_event.Id);
        plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-023").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
        rendering_refusals.Omitted(plan, _event.Id);
        rendering_refusals.Omitted(plan, reducer.Slice.Id);
    }
}
