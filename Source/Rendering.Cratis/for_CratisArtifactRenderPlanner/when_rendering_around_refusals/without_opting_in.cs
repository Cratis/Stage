// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class without_opting_in : given.a_refused_specification
{
    void Because() => _plan = _planner.Plan(_original);

    [Fact] void should_render_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_application() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath == "Program.cs");
    [Fact] void should_list_the_omitted_specification() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_omit_the_refused_source() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
    [Fact] void should_report_a_warning() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Artifact == _refused && diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning);
}
