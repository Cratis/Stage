// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

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
        if (!CratisArtifactRenderProfileAdmission.Matches(request, out var options, out var mismatch))
        {
            return ArtifactRenderPlan.Create(
                request,
                [],
                [Error(
                    "STAGE-CRATIS-001",
                    $"The artifact render profile is not the exact package-owned Cratis profile. {mismatch}",
                    request.Model.Application.Id)]);
        }

        try
        {
            return PlanAdmitted(request, options);
        }
        catch (InvalidTypedContext exception)
        {
            return ArtifactRenderPlan.Create(request, [], [Error("STAGE-ESM-021", exception.Message, request.Model.Application.Id)]);
        }
        catch (UnsupportedSemanticRendering exception)
        {
            return ArtifactRenderPlan.Create(
                request,
                [],
                [Error(exception.Code, exception.Message, request.Model.Application.Id)]);
        }
    }

    static ArtifactRenderPlan PlanAdmitted(ArtifactRenderRequest request, CratisRenderingOptions options)
    {
        var artifacts = new List<PlannedArtifact>();
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        if (!EsmSchemaV3Support.Supports(request.Model.LanguageVersion, request.Model.SemanticVersion))
        {
            return CreatePlan(request, [], [Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", request.Model.Application.Id)]);
        }
        diagnostics.AddRange(SemanticImplementationAdmission.Verify(request));
        diagnostics.AddRange(SemanticTypedContextAdmission.Verify(request));
        if (diagnostics.Count > 0)
        {
            return CreatePlan(request, [], diagnostics);
        }
        var context = new SemanticApplicationContext(request, options);
        var slices = context.SelectedSlices();
        diagnostics.AddRange(SemanticCratisAdmission.Evaluate(context, slices));
        foreach (var descriptor in request.TypedContextDescriptors)
        {
            try
            {
                // #119: validate wrappers fail-closed, but emit none until an admitted body consumes one.
                _ = SemanticTypedContextRenderer.Render(descriptor, context);
            }
            catch (InvalidTypedContext exception)
            {
                diagnostics.Add(Error("STAGE-ESM-021", exception.Message, request.Model.Application.Id));
            }
        }
        if (diagnostics.Exists(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return CreatePlan(request, [], diagnostics);
        }

        AddScaffold(request, context, artifacts, diagnostics);
        if (diagnostics.Exists(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return CreatePlan(request, [], diagnostics);
        }

        if (request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            // A caller that authored screens carries its own composition in the profile. A caller that did not
            // gets one composed from the model here, so a generated application is usable without a hand-written
            // screen. Either way the binding module accompanies it, because it is what the payload refers to.
            var composed = SceneCompositionInput.IsCarriedBy(request.Profile)
                ? null
                : DefaultSceneComposition.Create(context);
            if (composed is not null)
            {
                artifacts.Add(PlannedArtifact.CreateText(
                    SceneCompositionInput.RelativePath,
                    CanonicalSceneJson.Serialize(composed),
                    [context.Application.Id]));
            }

            if (composed is not null || SceneCompositionInput.IsCarriedBy(request.Profile))
            {
                artifacts.Add(PlannedArtifact.CreateText(
                    SceneBindingsRenderer.RelativePath,
                    SceneBindingsRenderer.Render(context),
                    [context.Application.Id]));
            }

            if (context.Strings is { } strings)
            {
                artifacts.Add(PlannedArtifact.CreateText(StringsCatalogInput.RelativePath, StringsCatalogInput.Render(strings, context.RootNamespace), [context.Application.Id]));
            }

            artifacts.AddRange(context.Application.Concepts.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
            artifacts.AddRange(context.Application.Types.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
            if (slices.Any(slice => slice.Slice.Commands.Any(command => command.Authorization is not null) ||
                slice.Slice.Queries.Any(query => query.Authorization is not null)))
            {
                artifacts.Add(Artifact(SemanticPolicyArtifactRenderer.Render(context, slices)));
            }
        }

        foreach (var located in slices)
        {
            var renderer = SemanticSliceArtifactRenderers.Ordered.FirstOrDefault(_ => _.Handles(located)) ??
                throw UnsupportedSemanticRendering.For(nameof(SemanticSliceKind), located.Slice.Kind);
            artifacts.AddRange(renderer.Render(located, context).Select(Artifact));
        }

        artifacts.AddRange(SemanticCratisAdmission.SelectedConstraints(context, slices)
            .Select(_ => Artifact(SemanticConstraintArtifactRenderer.Render(_.Slice, _.Constraint, context))));
        return CreatePlan(request, artifacts, diagnostics);
    }

    static void AddScaffold(
        ArtifactRenderRequest request,
        SemanticApplicationContext context,
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
            if (input.Name == StringsCatalogInput.Name)
            {
                continue;
            }

            if (CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact))
            {
                if (artifact!.RelativePath == "Program.cs" && context.Strings is { } strings)
                {
                    var source = System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
                    artifacts.Add(PlannedArtifact.CreateText(artifact.RelativePath, StringsCatalogInput.ConfigureProgram(source, strings)));
                }
                else
                {
                    artifacts.Add(artifact.RelativePath == SceneCompositionInput.RelativePath
                        ? PlannedArtifact.CreateText(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), [request.Model.Application.Id])
                        : artifact);
                }
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

        if (unique.Exists(artifact => artifact.RelativePath.StartsWith("Customizations/", StringComparison.OrdinalIgnoreCase)))
        {
            diagnostics.Add(Error(
                "STAGE-CRATIS-005",
                "The top-level Customizations directory is reserved for user-owned files. Rename the modeled module or feature that would render into it.",
                request.Model.Application.Id));
            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        return ArtifactRenderPlan.Create(request, [.. unique], [.. diagnostics]);
    }

    static PlannedArtifact Artifact(RenderedFile file) => PlannedArtifact.CreateText(file.RelativePath, file.Content, file.Sources);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
