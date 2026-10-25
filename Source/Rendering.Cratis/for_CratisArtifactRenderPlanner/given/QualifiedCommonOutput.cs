// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Restores the output Stage rendered before it resolved imports, so the previous frozen bytes remain an independent
/// gate proving that resolving imports and moving the using placement changed nothing else.
/// </summary>
internal static class QualifiedCommonOutput
{
    internal const string Placement = "csharp_using_directive_placement = outside_namespace\n";

    // Restores an application-scoped plan the way the request-less planners make it, in the fully qualified spelling.
    internal static ImmutableArray<PlannedArtifact> Restore(ExecutableSemanticModel model, ArtifactRenderPlan plan, CratisRenderingOptions options) =>
        Restore(plan, () => CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), options));

    // Restores the output as Stage rendered it before imports were resolved: the plan made again in the fully qualified
    // spelling, so a frozen digest of the old output proves that resolving imports changed spelling alone.
    internal static ImmutableArray<PlannedArtifact> Restore(ArtifactRenderPlan plan, Func<ArtifactRenderPlan> replan)
    {
        ArtifactRenderPlan qualified;
        using (QualifiedSpelling.Begin())
        {
            qualified = replan();
        }

        var files = qualified.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .ToDictionary(artifact => artifact.RelativePath, StringComparer.Ordinal);
        return [.. plan.Artifacts.Select(artifact => files.TryGetValue(artifact.RelativePath, out var file)
            ? PlannedArtifact.CreateText(artifact.RelativePath, Encoding.UTF8.GetString(file.Bytes.AsSpan()), artifact.Sources)
            : WithoutPlacement(artifact))];
    }

    static PlannedArtifact WithoutPlacement(PlannedArtifact artifact) => artifact.RelativePath == ".editorconfig"
        ? PlannedArtifact.CreateText(artifact.RelativePath, BeforePlacement(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
        : artifact;

    internal static string BeforePlacement(string text) => text.Replace(Placement, string.Empty, StringComparison.Ordinal);

    internal static string Digest(ExecutableSemanticModel model, ArtifactRenderPlan plan, CratisRenderingOptions options) =>
        FrozenScaffoldPins.Digest(plan, Restore(model, plan, options));
}
#endif
