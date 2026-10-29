// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_no_independent_slice : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish() => ChangeApplication(application => application with { Modules = [.. application.Modules.Where(module => module.Name != "Billing")] });
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_degraded_scaffold() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_not_emit_the_root_or_dependent_command() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_unsupportedEvent) || artifact.Sources.Contains(_registerProject.Commands.Single().Id)).ShouldBeFalse();
    [Fact] void should_not_compose_a_scene_from_omitted_owners() => _plan.Artifacts.Any(artifact => artifact.RelativePath == SceneCompositionInput.RelativePath).ShouldBeFalse();
    [Fact] void should_not_publish_a_runtime_scene_from_omitted_owners() => _plan.Artifacts.Any(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).ShouldBeFalse();
    [Fact] void should_not_report_skipped_specifications() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-PLAN-007").ShouldBeFalse();

}
