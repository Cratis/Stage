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
    protected ArtifactRenderPlan Plan()
    {
        var (plan, inputs) = PlanWithInputs(CanonicalSceneJson.Serialize(_scene));
        return RequireArtifacts(plan, inputs);
    }

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
    protected ArtifactRenderPlan Plan(string sceneJson) => PlanWithInputs(sceneJson).Plan;

    (ArtifactRenderPlan Plan, IEnumerable<ArtifactRenderInput> Inputs) PlanWithInputs(string sceneJson)
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        var withScene = ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, sceneJson)]);
        var plan = new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(
            _model,
            _executionPlan,
            withScene,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id)));
        return (plan, withScene.Inputs);
    }

    /// <summary>
    /// Plans the application from the authored Scene whose ui profile also lists <c language="text">PrimeReact</c>, the opt-in component library (Cratis/Stage#707).
    /// </summary>
    /// <returns>The plan.</returns>
    protected ArtifactRenderPlan PlanWithPrimeReact()
    {
        var scene = _scene with { UiProfiles = [.. _scene.UiProfiles.Select(profile => profile with { Packages = [.. profile.Packages, "PrimeReact"] })] };
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options, scene, null, null, null);
        var plan = new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(
            _model,
            _executionPlan,
            profile,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id)));
        return RequireArtifacts(plan, profile.Inputs);
    }

    /// <summary>
    /// Gets the text of one planned artifact.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="relativePath">The artifact's path.</param>
    /// <returns>The UTF-8 text.</returns>
    protected static string Text(ArtifactRenderPlan plan, string relativePath)
    {
        RequireArtifacts(plan, []);
        var matches = plan.Artifacts.Where(_ => _.RelativePath == relativePath).ToArray();
        Assert.True(matches.Length == 1, $"Expected one artifact at '{relativePath}', found {matches.Length}.{Environment.NewLine}{Diagnostics(plan)}");
        return System.Text.Encoding.UTF8.GetString(matches[0].Bytes.AsSpan());
    }

    /// <summary>
    /// Requires successful admission and generated artifacts.
    /// </summary>
    /// <param name="plan">The plan to check.</param>
    /// <param name="inputs">The inputs used to plan the artifacts.</param>
    /// <returns>The successful plan.</returns>
    protected static ArtifactRenderPlan RequireArtifacts(ArtifactRenderPlan plan, IEnumerable<ArtifactRenderInput> inputs)
    {
        if (plan.Success && plan.Artifacts.Length > 0) return plan;
        var rejected = inputs.Where(input => input.Name == ScreenPartitionsInput.Name || input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}")
            .SelectMany(RejectedGuards);
        Assert.Fail($"Expected successful admission and generated artifacts.{Environment.NewLine}{Diagnostics(plan)}{Environment.NewLine}" +
            $"guards rejected by SceneGuards admission (test-side re-evaluation){Environment.NewLine}{string.Join(Environment.NewLine, rejected)}");
        return plan;
    }

    static string Diagnostics(ArtifactRenderPlan plan) => string.Join(Environment.NewLine,
        plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Message} | {diagnostic.Artifact}"));

    static IEnumerable<string> RejectedGuards(ArtifactRenderInput input)
    {
        try
        {
            return [.. RejectedGuards(JsonNode.Parse(input.Bytes.AsSpan())).Select(guard => $"{input.Name}: {guard}")];
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            return [$"{input.Name}: guard re-evaluation unavailable ({exception.GetType().Name})"];
        }
    }

    protected static IEnumerable<string> RejectedGuards(JsonNode? node, string jsonPath = "")
    {
        if (node is JsonObject element)
        {
            if (element.ContainsKey("trigger") && element["condition"] is JsonObject bindingCondition && !SceneGuards.CanRunBinding(bindingCondition))
            {
                yield return $"{jsonPath}/condition: kind={Spelling(bindingCondition["kind"])}, operator={Spelling(bindingCondition["operator"])}";
            }
            if (element["alternatives"] is JsonArray alternatives)
            {
                for (var index = 0; index < alternatives.Count; index++)
                {
                    var condition = alternatives[index] is JsonObject branch ? branch["condition"] : null;
                    foreach (var rejection in RejectedCondition(condition, $"{jsonPath}/alternatives/{index}/condition")) yield return rejection;
                }
            }
            foreach (var property in element)
            {
                var segment = property.Key.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
                foreach (var rejection in RejectedGuards(property.Value, $"{jsonPath}/{segment}")) yield return rejection;
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                foreach (var rejection in RejectedGuards(array[index], $"{jsonPath}/{index}")) yield return rejection;
            }
        }
    }

    static IEnumerable<string> RejectedCondition(JsonNode? condition, string jsonPath)
    {
        if (SceneGuards.CanEvaluate(condition)) yield break;
        var guard = condition as JsonObject;
        yield return $"{jsonPath}: kind={Spelling(guard?["kind"])}, operator={Spelling(guard?["operator"])}";
        if (guard?["kind"] is not JsonValue kind || !kind.TryGetValue<string>(out var name) || name != "logical") yield break;
        foreach (var side in new[] { "left", "right" })
        {
            foreach (var rejection in RejectedCondition(guard[side], $"{jsonPath}/{side}")) yield return rejection;
        }
    }

    static string Spelling(JsonNode? value) => value switch
    {
        null => "<absent>",
        JsonValue scalar when scalar.TryGetValue<string>(out _) || scalar.TryGetValue<double>(out _) => scalar.ToJsonString(),
        _ => "<non-scalar>"
    };

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
