// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_an_evolved_event_and_an_unsupported_specification : given.a_collecting_render_request
{
    SemanticId _event;
    SemanticId _specification;

    void Establish()
    {
        var declarations = string.Join('\n', Enumerable.Range(1, 2).Select(revision =>
            $"      event ProjectRegistered generation {revision}\n        projectId ProjectId\n        name ProjectName"));
        var source = Source.Replace("      event ProjectRegistered\n        projectId ProjectId\n        name ProjectName", declarations, StringComparison.Ordinal)
            .Replace("      specification RegisteringAProject\n", """
                      specification RegisteringAProject
                        given readmodel ProjectSummary
                          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                          name = "Screenplay"
                """ + "\n", StringComparison.Ordinal);
        _request = WithSource(source);
        var slice = _request.Model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange);
        _event = slice.Events.Single().Id;
        _specification = slice.Specifications.Single(specification => specification.Name == "RegisteringAProject").Id;
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_the_event_revision() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-026" && diagnostic.Artifact == _event);
    [Fact] void should_also_report_specification_admission() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _specification);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
