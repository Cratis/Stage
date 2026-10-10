// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
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
/// the authored Scene - and the Scene travels to the planner as the <c language="json">scene.json</c> input with its
/// guarded actions removed, which is the runnable subset the CLI hands to Stage. A Scene that still carried them
/// would be refused by the planner, and that refusal is specified on its own.
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
    /// Plans the application from the runnable Scene, as <c language="shell">cratis render</c> hands it to Stage.
    /// </summary>
    /// <returns>The plan.</returns>
    protected ArtifactRenderPlan Plan() => Plan(RunnableSceneJson(_scene));

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

    /// <summary>
    /// The runnable Scene payload: the authored Scene with every guarded action removed, as the CLI produces it.
    /// </summary>
    /// <param name="scene">The translated authored Scene.</param>
    /// <returns>The Scene JSON without guarded actions.</returns>
    protected static string RunnableSceneJson(SceneApplication scene)
    {
        var node = JsonNode.Parse(CanonicalSceneJson.Serialize(scene))!;
        RemoveGuardedActions(node);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    static void RemoveGuardedActions(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            for (var index = array.Count - 1; index >= 0; index--)
            {
                if (IsGuardedAction(array[index]))
                {
                    array.RemoveAt(index);
                    continue;
                }

                RemoveGuardedActions(array[index]);
            }
        }
        else if (node is JsonObject @object)
        {
            foreach (var property in @object.ToArray())
            {
                RemoveGuardedActions(property.Value);
            }
        }
    }

    static bool IsGuardedAction(JsonNode? node) =>
        node is JsonObject @object &&
        @object["componentName"]?.GetValue<string>() == "core:action" &&
        @object["properties"] is JsonObject properties &&
        (properties.ContainsKey("alternatives") || properties.ContainsKey("otherwise"));

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
