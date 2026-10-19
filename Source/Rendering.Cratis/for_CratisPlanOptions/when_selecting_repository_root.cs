// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_repository_root : a_register_project_render_request
{
    const string Workflow = ".github/workflows/build.yml";

    CratisPlanOptions _options_for_plan = null!;
    CratisPlanResult _default = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _application = null!;
    CratisPlanResult _scoped = null!;

    void Establish() => _options_for_plan = new(_model.Application.Name, "Projects", "Projects");

    void Because()
    {
        var loaded = new LoadedSemanticModel(_model, _executionPlan);
        var withRoot = _options_for_plan with { RepositoryRoot = true };
        _default = CratisRendering.PlanScaffold(_options_for_plan);
        _scaffold = CratisRendering.PlanScaffold(withRoot);
        _application = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module(_module.Name)]), withRoot);
        _scoped = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]), withRoot);
    }

    [Fact] void should_default_to_a_non_repository_root() => _options_for_plan.RepositoryRoot.ShouldBeFalse();
    [Fact] void should_not_change_the_default_profile_roster() => CratisRendering.CreateProfile(_model.Application.Name, _options).Inputs.Any(input => input.Name.Contains(".github/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_change_the_default_scaffold_plan() => _default.Artifacts.Any(artifact => artifact.RelativePath.StartsWith(".github/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_plan_the_workflow_in_the_scaffold() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(Workflow);
    [Fact] void should_plan_the_dependency_updates_in_the_scaffold() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(".github/dependabot.yml");
    [Fact] void should_succeed_for_the_scaffold() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_plan_the_workflow_for_the_application_scope() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(Workflow);
    [Fact] void should_not_plan_the_workflow_for_a_scoped_plan() => _scoped.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain(Workflow);
}
