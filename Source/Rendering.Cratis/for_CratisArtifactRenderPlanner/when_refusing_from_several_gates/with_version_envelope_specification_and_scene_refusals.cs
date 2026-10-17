// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_version_envelope_specification_and_scene_refusals : given.a_collecting_render_request
{
    SemanticId _specification;

    void Establish()
    {
        var source = Source.Replace(
                "      specification RejectingAnEmptyProjectName",
                """
                        then no readmodel ProjectSummary for "4fa85f64-5717-4562-b3fc-2c963f66afa7"
                      specification RejectingAnEmptyProjectName
                """,
                StringComparison.Ordinal);
        _request = WithGuard(WithSource(source)) with { ImplementationRequirements = default };
        _specification = _request.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(slice => slice.Specifications).Single(specification => specification.Name == "RegisteringAProject").Id;
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_list_the_version_feature() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-027" && diagnostic.Artifact == _specification);
    [Fact] void should_list_the_missing_envelope() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == _model.Application.Id);
    [Fact] void should_list_the_specification_refusal() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _specification);
    [Fact] void should_list_the_scene_guard() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_list_each_refusal_once() => _plan.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Artifact, diagnostic.Message)).Distinct().Count().ShouldEqual(_plan.Diagnostics.Length);
    [Fact] void should_report_failure() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
