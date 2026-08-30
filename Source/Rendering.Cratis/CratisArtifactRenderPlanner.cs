// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Plans deterministic Cratis application artifacts directly from the Screenplay executable semantic model.
/// </summary>
public sealed class CratisArtifactRenderPlanner : IArtifactRenderPlanner
{
    /// <summary>
    /// The stable target identity.
    /// </summary>
    public const string Target = CratisRendering.TargetId;

    /// <summary>
    /// The stable renderer identity.
    /// </summary>
    public const string Renderer = CratisRendering.RendererId;

    /// <summary>
    /// The exact renderer implementation version.
    /// </summary>
    public const string RendererVersion = CratisRendering.RendererVersion;

    /// <summary>
    /// Gets the exact supported Cratis integration target version.
    /// </summary>
    public static string TargetVersion => CratisRendering.TargetVersion;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(ArtifactRenderRequest request)
    {
        var artifacts = new List<PlannedArtifact>();
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        if (!CratisArtifactRenderProfileAdmission.Matches(request, out var mismatch))
        {
            diagnostics.Add(Error(
                "STAGE-CRATIS-001",
                $"The artifact render profile is not the exact package-owned Cratis profile. {mismatch}",
                request.Model.Application.Id));
            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        var context = new SemanticApplicationContext(request);
        var slices = context.SelectedSlices();
        diagnostics.AddRange(SemanticCratisAdmission.Evaluate(context, slices));
        if (diagnostics.Exists(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return CreatePlan(request, [], diagnostics);
        }

        AddScaffold(request, artifacts, diagnostics);
        if (diagnostics.Exists(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return CreatePlan(request, [], diagnostics);
        }

        if (request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            artifacts.AddRange(context.Application.Concepts.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
            artifacts.AddRange(context.Application.Types.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
        }

        foreach (var located in slices)
        {
            var file = located.Slice.Kind switch
            {
                SemanticSliceKind.StateChange => SemanticStateChangeArtifactRenderer.Render(located, context),
                SemanticSliceKind.StateView => SemanticStateViewArtifactRenderer.Render(located, context),
                _ => null
            };
            if (file is not null)
            {
                artifacts.Add(Artifact(file));
            }

            foreach (var specification in located.Slice.Specifications)
            {
                artifacts.AddRange(SemanticSpecificationArtifactRenderer.Render(specification, context).Select(Artifact));
            }
        }

        return CreatePlan(request, artifacts, diagnostics);
    }

    static void AddScaffold(
        ArtifactRenderRequest request,
        List<PlannedArtifact> artifacts,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (request.Scope.Kind != ArtifactRenderScopeKind.Application)
        {
            return;
        }

        var count = 0;
        foreach (var input in request.Profile.Inputs)
        {
            if (CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact))
            {
                artifacts.Add(artifact!);
                count++;
            }
            else
            {
                diagnostics.Add(Error(
                    "STAGE-CRATIS-002",
                    $"Renderer input '{input.Name}' is not a recognized Cratis scaffold artifact.",
                    request.Model.Application.Id));
            }
        }

        if (count == 0)
        {
            diagnostics.Add(Error(
                "STAGE-CRATIS-003",
                "Application planning requires at least one fully resolved Cratis scaffold artifact input.",
                request.Model.Application.Id));
        }
    }

    static ArtifactRenderPlan CreatePlan(
        ArtifactRenderRequest request,
        IEnumerable<PlannedArtifact> artifacts,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var unique = new List<PlannedArtifact>();
        foreach (var group in artifacts.GroupBy(_ => _.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            unique.Add(group.OrderBy(_ => _.RelativePath, StringComparer.Ordinal).First());
            if (group.Count() > 1)
            {
                diagnostics.Add(Error(
                    "STAGE-CRATIS-004",
                    $"Generated artifact path '{group.Key}' collides with another scaffold or semantic artifact.",
                    request.Model.Application.Id));
            }
        }

        return ArtifactRenderPlan.Create(request, [.. unique], [.. diagnostics]);
    }

    static PlannedArtifact Artifact(RenderedFile file) => PlannedArtifact.CreateText(file.RelativePath, file.Content);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
