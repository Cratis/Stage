// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class without_authored_screens : a_register_project_render_request
{
    static readonly string[] _aggregates = ["scene.json", "src/bindings.ts", "src/stage-scene.json", "src/stage.ts"];

    CratisPlanResult _command = null!;
    CratisPlanResult _both = null!;
    CratisPlanResult _without = null!;
    ArtifactRenderPlan _application = null!;

    void Because()
    {
        var loaded = new LoadedSemanticModel(_model, _executionPlan);
        var options = new CratisPlanOptions(_model.Application.Name, _options.ProjectName, _options.RootNamespace);
        var command = new PlanSelection([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]);
        _command = CratisRendering.PlanFrom(loaded, command, options with { UserInterface = new([]) });
        _both = CratisRendering.PlanFrom(loaded, command, options with { UserInterface = new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _projectLookup.Name)]) });
        _without = CratisRendering.PlanFrom(loaded, command, options);
        _application = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);
    }

    [Fact] void should_admit_the_plans() => (_command.Success && _both.Success && _without.Success).ShouldBeTrue();
    [Fact] void should_compose_a_default_screen_for_the_selected_slice() => _aggregates.All(path => _command.Artifacts.Any(artifact => artifact.RelativePath == path)).ShouldBeTrue();
    [Fact] void should_bind_only_the_command_that_is_present() => Text(_command.Artifacts.Single(artifact => artifact.RelativePath == "src/bindings.ts")).ShouldNotContain(_projectLookup.Queries.Single().Name);
    [Fact] void should_match_the_application_aggregates_when_every_slice_is_present() => _aggregates.Select(path => _both.Artifacts.Single(artifact => artifact.RelativePath == path).Sha256).ShouldEqual(_aggregates.Select(path => _application.Artifacts.Single(artifact => artifact.RelativePath == path).Sha256));
    [Fact] void should_keep_scoped_plans_without_the_option_free_of_shared_ui() => _without.Artifacts.Any(artifact => _aggregates.Contains(artifact.RelativePath)).ShouldBeFalse();
}
