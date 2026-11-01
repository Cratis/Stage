// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_observing_a_successful_render_with_refused_specifications : Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals.given.a_refused_specification
{
    conformance_observation _observation = null!;

    async Task Because()
    {
        _plan = _planner.Plan(_original);
        _observation = await conformance_outcomes.Execute(conformance_surface.Renderer,
            new("RegisterProject.SpecificationOmission", "regression", _original.Model, null));
    }

    [Fact] void should_render_the_remaining_application_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_application_artifacts() => _plan.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_identify_the_omitted_specification() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_observe_a_refused_cell() => _observation.Outcome.ShouldEqual("refused");
    [Fact] void should_preserve_the_code_and_artifact_without_a_kind() => _observation.Refusals.ShouldContainOnly([new refusal_fingerprint("STAGE-ESM-011", null, _refused.ToString())]);
    [Fact]
    void should_use_the_matching_warning_message() => _observation.Details[new("STAGE-ESM-011", null, _refused.ToString())].ShouldEqual(
        _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _refused && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).Message);
}
#endif
