// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// An application that declares no screens still gets one, composed from its own model.
/// </summary>
/// <remarks>
/// Without this, generating an application produces a frontend shell with nothing in it, and the only way to
/// exercise the backend that was just generated is to write the screen by hand first.
/// </remarks>
public class when_planning_an_application_without_screens : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.ShouldBeEmpty();

    [Fact] void should_compose_a_default_scene() => Artifact(SceneCompositionInput.RelativePath).ShouldNotBeNull();

    [Fact] void should_emit_the_binding_module_for_it() => Artifact(SceneBindingsRenderer.RelativePath).ShouldNotBeNull();

    [Fact] void should_place_the_command_on_the_composed_screen() =>
        Text(Artifact(SceneCompositionInput.RelativePath)!).ShouldContain("\"command\":\"RegisterProject\"");

    [Fact] void should_bind_the_composed_command_form() =>
        Text(Artifact(SceneBindingsRenderer.RelativePath)!).ShouldContain("registerCommands({RegisterProject});");

    /// <summary>
    /// The released single-result component takes its argument from a host that has committed it, and there is
    /// no editable input binding yet (Cratis/Scene#39), so a composed keyed lookup could only render its idle
    /// state. Composing one would put a permanently inert element on every generated screen.
    /// </summary>
    [Fact] void should_not_compose_a_keyed_lookup_it_cannot_drive_yet() =>
        Text(Artifact(SceneCompositionInput.RelativePath)!).ShouldNotContain("singleResult");

    PlannedArtifact? Artifact(string relativePath) =>
        _plan.Artifacts.FirstOrDefault(_ => string.Equals(_.RelativePath, relativePath, StringComparison.Ordinal));
}
