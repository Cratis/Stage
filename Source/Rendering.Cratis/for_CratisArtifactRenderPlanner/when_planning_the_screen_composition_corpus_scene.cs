// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Packages;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_the_screen_composition_corpus_scene : Specification
{
    SceneApplication _folderScene = null!;
    ApplicationRenderPlan _folderPlan = null!;
    SceneApplication _typedScene = null!;
    ApplicationRenderPlan _typedPlan = null!;

    void Because()
    {
        _folderScene = SceneFor(ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder"));
        _folderPlan = RenderPlanner.Plan(_folderScene, Catalog);

        var typedCase = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "screen-release-ui-positive");
        _typedScene = SceneFor(typedCase.Document);
        _typedPlan = RenderPlanner.Plan(_typedScene, Catalog);
    }

    [Fact] void should_plan_the_folder_scene_without_findings() => _folderPlan.Findings.ShouldBeEmpty();
    [Fact] void should_plan_the_typed_scene_without_findings() => _typedPlan.Findings.ShouldBeEmpty();
    [Fact] void should_preserve_the_folder_navigation_contribution() => _folderScene.Screens.SelectMany(_ => _.Contributions).Any(_ => _.ContributionPointName == "Navigation").ShouldBeTrue();
    [Fact] void should_preserve_dialog_template_metadata() => _typedScene.DialogTemplates.Single(_ => _.Name == "EditWorkItemDialog").Metadata!.Type.ShouldEqual("commandForm");
    [Fact] void should_preserve_layout_outlet_metadata() => _typedScene.Layouts.Single().Outlets!.Single().Name.ShouldEqual("details");
    [Fact] void should_preserve_package_profile_choices() => string.Join(',', _typedScene.UiProfiles.Single().Packages).ShouldEqual("core,scene.web,Cratis.Components");
    [Fact] void should_emit_the_typed_grid_component() => TypedGrid.ComponentName.ShouldEqual("scene.web.DataGrid");
    [Fact] void should_preserve_component_property_bindings() => SelectedItemBinding.Kind.ShouldEqual(BindingSourceKind.ComponentProperty);
    [Fact] void should_preserve_component_property_null_behavior() => SelectedItemBinding.NullBehavior.ShouldEqual(BindingNullBehavior.Clear);
    [Fact] void should_preserve_component_exposures() => ((Dictionary<string, BindingExpression>)TypedGrid.Properties["exposes"]!).ContainsKey("selectedWorkItem").ShouldBeTrue();
    [Fact] void should_preserve_component_outlets() => TypedGrid.Slots.ContainsKey("detail").ShouldBeTrue();
    [Fact] void should_emit_toolbar_items_with_icons() => Toolbar.Slots["items"].Cast<ExternalComponent>().Any(_ => (string?)_.Properties["icon"] == "add").ShouldBeTrue();

    ExternalComponent TypedGrid => _typedScene.Screens.Single(_ => _.Name == "WorkItemList").SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single(_ => _.ComponentName == "scene.web.DataGrid");
    BindingExpression SelectedItemBinding => (BindingExpression)TypedGrid.Properties["selectedItem"]!;
    ExternalComponent Toolbar => _typedScene.Screens.Single(_ => _.Name == "WorkItemList").SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single(_ => _.ComponentName == "core:toolbar");

    static SceneApplication SceneFor(CanonicalCorpusSourceForm form) => SceneFor(form.Documents);
    static SceneApplication SceneFor(CanonicalCorpusDocument document) => SceneFor([document]);

    static SceneApplication SceneFor(IEnumerable<CanonicalCorpusDocument> documents)
    {
        var root = Path.Combine(Path.GetTempPath(), $"stage-screen-corpus-{Guid.NewGuid():N}");
        try
        {
            foreach (var document in documents)
            {
                var path = Path.Combine(root, document.DisplayPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, document.Text);
            }

            var compilation = new PlayFileCompiler().CompileFolder(root).Result;
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return new ScreenplaySceneVisitor().Visit(compilation.Value!);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    static IReadOnlyList<ScenePackage> Catalog =>
    [
        new("core", "1.0.0", PackageKind.ComponentLibrary, [], ["core:contribution", "core:data", "core:action", "core:section", "core:title", "core:table", "core:column", "core:summary", "core:field", "core:toolbar", "core:toolbarItem"], [], [], [], []),
        new("scene.web", "1.0.0", PackageKind.ComponentLibrary, [], ["scene.web.DataGrid", "scene.web.DetailsPanel", "scene.web.CommandForm"], [], [], [], []),
        new("Cratis.Components", "4.26.2", PackageKind.ComponentLibrary, [], [], [], [], [], []),
    ];
}
