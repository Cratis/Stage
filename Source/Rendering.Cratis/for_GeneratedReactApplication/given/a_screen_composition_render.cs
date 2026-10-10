// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Plans the canonical screen-composition corpus the way <c language="shell">cratis render</c> does.
/// </summary>
/// <remarks>
/// The corpus is compiled twice from the same folder form - once into the executable semantic model, once into
/// the authored Scene - and the Scene travels to the planner as the <c language="json">scene.json</c> input as authored,
/// with its guarded Close action and its guarded double click, which the Stage runtime evaluates.
/// </remarks>
public class a_screen_composition_render : Specification
{
    protected static readonly CanonicalCorpusSourceForm Folder = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");

    protected ExecutableSemanticModel _model = null!;
    protected SemanticExecutionPlan _executionPlan = null!;
    protected SceneApplication _scene = null!;
    protected CratisRenderingOptions _options = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(Folder.IdentityCatalogBytes.AsSpan());
        var documents = Folder.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            document.Text));
        var compilation = new SemanticModelCompiler().Compile(
            ScreenCompositionCorpus.V1.ApplicationName,
            SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        _model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(_model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _executionPlan = execution.Plan!;
        _scene = SceneFor(Folder.Documents);
        _options = new(_model.Application.Name, _model.Application.Name);
    }

    /// <summary>
    /// Plans the application from the authored Scene, guarded actions and interactions included.
    /// </summary>
    /// <returns>The plan.</returns>
    protected ArtifactRenderPlan Plan() => Plan(CanonicalSceneJson.Serialize(_scene));

    /// <summary>
    /// Writes every planned artifact beneath a directory, at its relative path.
    /// </summary>
    /// <param name="plan">The plan to write.</param>
    /// <param name="directory">The directory to write beneath.</param>
    protected static void Write(ArtifactRenderPlan plan, string directory)
    {
        foreach (var artifact in plan.Artifacts)
        {
            var path = Path.Combine(directory, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. artifact.Bytes]);
        }
    }

    /// <summary>
    /// Plans the application from a Scene payload.
    /// </summary>
    /// <param name="sceneJson">The Scene payload carried as the <c language="json">scene.json</c> input.</param>
    /// <returns>The plan.</returns>
    protected ArtifactRenderPlan Plan(string sceneJson)
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        var withScene = ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, sceneJson)]);
        return new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(
            _model,
            _executionPlan,
            withScene,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id)));
    }

    /// <summary>
    /// Gets the text of one planned artifact.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="relativePath">The artifact's path.</param>
    /// <returns>The UTF-8 text.</returns>
    protected static string Text(ArtifactRenderPlan plan, string relativePath) =>
        System.Text.Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath == relativePath).Bytes.AsSpan());

    static SceneApplication SceneFor(IEnumerable<CanonicalCorpusDocument> documents)
    {
        var root = Contracts.Specs.SpecTemporaryRoot.NewPath("stage-screen-corpus");
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
            Contracts.Specs.SpecTemporaryRoot.Delete(root);
        }
    }
}
