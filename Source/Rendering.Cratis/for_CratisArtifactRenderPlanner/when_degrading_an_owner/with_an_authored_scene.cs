// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_an_authored_scene : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        const string json = """
            {"uiProfiles":[],"themes":[],"layouts":[],"screenTemplates":[],"dialogTemplates":[],"screens":[{"name":"Projects","slotContent":{"content":[{"componentName":"core:command-form","properties":{"command":"RegisterProject"}}]}}]}
            """;
        var profile = _request.Profile;
        _request = _request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, json)])
        };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_refuse_the_whole_render() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_not_publish_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_publish_removals() => _plan.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_keep_the_original_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_record_degradation() => _plan.Degradations.ShouldBeEmpty();
}
