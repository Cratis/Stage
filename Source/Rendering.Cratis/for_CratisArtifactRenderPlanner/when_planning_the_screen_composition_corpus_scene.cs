// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    Exception _typedError = null!;

    void Because()
    {
        _folderScene = SceneFor(ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder"));
        _folderPlan = RenderPlanner.Plan(_folderScene, Catalog);

        var typedCase = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "screen-release-ui-positive");
        _typedError = Catch.Exception(() => SceneFor(typedCase.Document));
    }

    [Fact] void should_plan_the_folder_scene_without_findings() => _folderPlan.Findings.ShouldBeEmpty();
    [Fact] void should_refuse_the_typed_scene_before_planning() => _typedError.ShouldBeOfExactType<UnsupportedUiSyntax>();
    [Fact] void should_name_the_unsupported_navigation_field() => ((UnsupportedUiSyntax)_typedError).Member.ShouldEqual("ContributionSyntax.Id");
    [Fact] void should_report_the_typed_refusal_code() => _typedError.Message.ShouldContain(UnsupportedUiSyntax.DiagnosticCode);
    [Fact] void should_preserve_the_folder_navigation_contribution() => _folderScene.Screens.SelectMany(_ => _.Contributions).Any(_ => _.ContributionPointName == "Navigation").ShouldBeTrue();
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
