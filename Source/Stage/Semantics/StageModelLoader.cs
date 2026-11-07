// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// A problem found while loading a model that stops the Stage serving it.
/// </summary>
/// <param name="Capability">The kind of capability that is unsupported.</param>
/// <param name="Artifact">The artifact the problem belongs to.</param>
/// <param name="Details">What is wrong.</param>
internal sealed record StageLoadIssue(string Capability, string Artifact, string Details);

/// <summary>
/// Compiles a model from a path into its executable semantic form and the scene it renders.
/// </summary>
internal static class StageModelLoader
{
    /// <summary>
    /// Loads a model, collecting everything that stops it being served instead of throwing.
    /// </summary>
    /// <param name="modelPath">A <c language="shell">.play</c> file or a folder of them.</param>
    /// <param name="issues">Receives the problems that stop the model being served.</param>
    /// <param name="notes">Receives the load diagnostics, when supplied.</param>
    /// <param name="applicationName">The application name, or <see langword="null"/> to take it from the path.</param>
    /// <returns>The model and scene, or <see langword="null"/> for both when the model cannot be served.</returns>
    internal static async Task<(LoadedSemanticModel? Model, SceneApplication? Scene)> Load(string modelPath, List<StageLoadIssue> issues, List<SemanticModelLoadDiagnostic>? notes = null, string? applicationName = null)
    {
        try
        {
            var loaded = await SemanticModelLoader.LoadFromPathAsync(modelPath, null, applicationName);
            notes?.AddRange(loaded.Notes);

            var scene = SemanticHostScene.Load(modelPath, loaded.Model);
            if (scene.RuntimeIssues.All(SafeSceneApplication.CanServeWithoutUnsafeBehavior))
            {
                return (loaded, SafeSceneApplication.From(scene));
            }

            issues.AddRange(scene.RuntimeIssues.Select(issue => new StageLoadIssue("Scene", issue.Artifact, issue.Details)));

            return scene.RuntimeIssues.Count > 0 ? (null, null) : (loaded, scene);
        }
        catch (UnsupportedUiSyntax exception)
        {
            issues.Add(new StageLoadIssue("Scene", "model", exception.Message));

            return (null, null);
        }
        catch (InvalidSemanticModel invalid)
        {
            notes?.AddRange(invalid.Notes);
            issues.AddRange(invalid.Diagnostics.Select(message => new StageLoadIssue("Plan", "model", message)));
            if (issues.Count == 0)
            {
                issues.Add(new StageLoadIssue("Plan", "model", "The semantic model could not be loaded."));
            }

            return (null, null);
        }
    }
}
