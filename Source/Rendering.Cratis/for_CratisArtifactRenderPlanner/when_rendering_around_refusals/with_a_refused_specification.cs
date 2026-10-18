// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_refused_specification : given.a_refused_specification
{
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_partial_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_success_false() => _plan.Success.ShouldBeFalse();
    [Fact] void should_list_the_omitted_specification() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_name_the_kind() => _plan.Refusals.Single().Kind.ShouldEqual("specification");
    [Fact] void should_keep_the_refusal_code() => _plan.Refusals.Single().Codes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_keep_the_original_error() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Artifact == _refused && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_explain_the_partial_outcome() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-PLAN-007" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information && diagnostic.Message == "Rendered around 1 refused specifications");
    [Fact] void should_emit_the_application() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath == "Program.cs");
    [Fact] void should_omit_every_refused_source() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
    [Fact] void should_keep_the_supported_specifications() => _plan.Artifacts.ShouldContain(artifact => artifact.Sources.Contains(_registerProject.Specifications.Single(specification => specification.Name == "RejectingAnEmptyProjectName").Id));
}
