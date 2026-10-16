// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Restores only the two authorized output changes so the previous frozen bytes remain an independent confinement gate.
/// </summary>
internal static class QualifiedCommonOutput
{
    internal const string Placement = "csharp_using_directive_placement = inside_namespace\n";

    internal static ImmutableArray<PlannedArtifact> Restore(ExecutableSemanticModel model, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var common = model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context, qualified: true))
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context, qualified: true)))
            .ToDictionary(file => file.RelativePath, StringComparer.Ordinal);
        return [.. plan.Artifacts.Select(artifact => common.TryGetValue(artifact.RelativePath, out var file)
            ? PlannedArtifact.CreateText(artifact.RelativePath, file.Content, artifact.Sources)
            : artifact.RelativePath == ".editorconfig"
                ? PlannedArtifact.CreateText(artifact.RelativePath, BeforePlacement(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
                : artifact)];
    }

    internal static string BeforePlacement(string text) => text.Replace(Placement, string.Empty, StringComparison.Ordinal);

    internal static string Digest(ExecutableSemanticModel model, ArtifactRenderPlan plan, CratisRenderingOptions options) => ArtifactRenderPlan.ComputeDigest(
        plan.Target, plan.TargetVersion, plan.Renderer, plan.RendererVersion, plan.ApplicationName, Restore(model, plan, options), plan.Diagnostics);
}
#endif
