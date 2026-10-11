// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_scene_composition : given.a_profile_source_plan
{
    CratisPlanResult _scaffold = null!;

    protected override SceneApplication? ComposedScene
    {
        get
        {
            var layout = DefaultLayout.Create();
            var screen = new SceneScreens.Screen("Projects", layout.Name, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal), [], []);

            return new([], [], [layout], [], [], [screen]);
        }
    }

    void Establish()
    {
        _selection = new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]);
        PlanFull();
    }

    void Because()
    {
        _scaffold = CratisRendering.PlanScaffold(_options);
        _result = CratisRendering.PlanFrom(_loaded, _selection, _options);
    }

    [Fact] void should_plan_the_scaffold_successfully() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_plan_the_slice_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_a_successful_full_baseline() => _full.Success.ShouldBeTrue();
    [Fact] void should_report_application_only_scene_output() => _scaffold.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-PLAN-031").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_omit_scene_files_from_the_scaffold() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).Intersect([SceneCompositionInput.RelativePath, SceneBindingsRenderer.RelativePath, StageSceneDocument.RelativePath, StageModuleRenderer.RelativePath]).ShouldBeEmpty();
    [Fact] void should_keep_scene_files_in_the_full_plan() => _full.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(SceneCompositionInput.RelativePath);
    [Fact] void should_match_every_scoped_full_render_artifact_byte() => ShouldMatchFull();
    [Fact] void should_match_every_scaffold_full_render_artifact_byte() => _scaffold.Artifacts.All(artifact => _full.Artifacts.Single(full => full.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
}
