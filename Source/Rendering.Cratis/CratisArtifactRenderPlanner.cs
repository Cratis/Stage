// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
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
        var plan = PlanCore(request);
        var warnings = ReleaseBaselineWarnings.For(request);

        return warnings.IsEmpty ? plan : ArtifactRenderPlan.Create(request, plan.Artifacts, [.. plan.Diagnostics, .. warnings]);
    }

    static ArtifactRenderPlan PlanCore(ArtifactRenderRequest request)
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
        if (!SemanticVersionFeatures.Supports(request.Model))
        {
            return CreatePlan(request, [], [Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", request.Model.Application.Id)]);
        }

        // Constructs a later version adds refuse the plan before any envelope or rendering check runs.
        var context = new SemanticApplicationContext(request, options);
        var slices = context.SelectedSlices();
        diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, slices));
        if (diagnostics.Count > 0)
        {
            return CreatePlan(request, [], diagnostics);
        }
        diagnostics.AddRange(SemanticImplementationAdmission.Verify(request));
        diagnostics.AddRange(SemanticTypedContextAdmission.Verify(request));
        diagnostics.AddRange(SemanticEventLineageAdmission.Verify(request));
        if (diagnostics.Count > 0)
        {
            return CreatePlan(request, [], diagnostics);
        }
        diagnostics.AddRange(SemanticCratisAdmission.Evaluate(context, slices));
        var selectedReducers = slices.SelectMany(_ => _.Slice.Reducers)
            .SelectMany(reducer => reducer.Transitions.Select(transition => (transition.RequirementId, reducer.ReadModel)))
            .ToHashSet();
        foreach (var descriptor in request.TypedContextDescriptors)
        {
            try
            {
                var wrapper = SemanticTypedContextRenderer.Render(descriptor, context);
                if (descriptor.OperationId is { } operation && selectedReducers.Contains((descriptor.RequirementId, operation)))
                {
                    artifacts.Add(Artifact(wrapper));
                }
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

        if (GeneratedTypeNames.UsesCommandReceiptTime(slices.SelectMany(located => located.Slice.Commands)))
        {
            artifacts.Add(Artifact(CommandReceiptTimeRendering.Render(context.RootNamespace)));
        }

        if (GeneratedTypeNames.UsesStreamIds(slices.Select(slice => slice.Slice)))
        {
            artifacts.Add(Artifact(StreamIdsRendering.Render(context.RootNamespace)));
        }

        if (selectedReducers.Count > 0)
        {
            artifacts.Add(Artifact(SemanticReducerContextRuntime.Render(context)));
            artifacts.Add(Artifact(TenantTranslationSource.Render(context.RootNamespace)));
        }

        ScreenPartitionsInput.TryRead(request.Profile, request.Model, out var partitions, out _);
        AddScaffold(request, context, artifacts, diagnostics, partitions);
        if (diagnostics.Exists(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return CreatePlan(request, [], diagnostics);
        }

        if (partitions is not null)
        {
            artifacts.AddRange(ScreenPartitionsInput.Render(partitions, slices));
            foreach (var slice in slices.Where(slice => !partitions.Entries.Exists(entry => entry.Owner == slice.Slice.Id.ToString())))
            {
                diagnostics.Add(new("STAGE-SCREEN-005", ArtifactRenderDiagnosticSeverity.Information, "This slice declares no authored screen; no slice-owned default UI fragment is emitted.", slice.Slice.Id));
            }

            if (request.Scope.Kind != ArtifactRenderScopeKind.Application)
            {
                diagnostics.Add(new("STAGE-SCREEN-006", ArtifactRenderDiagnosticSeverity.Information, "Application Scene aggregates are omitted from scoped plans; use application scope to publish shared UI.", context.Application.Id));
            }
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

                // The document and module the emitted Stage runtime renders the composition from.
                var sceneJson = composed is not null
                    ? CanonicalSceneJson.Serialize(composed)
                    : System.Text.Encoding.UTF8.GetString(artifacts.Single(_ => _.RelativePath == SceneCompositionInput.RelativePath).Bytes.AsSpan());
                artifacts.Add(PlannedArtifact.CreateText(
                    StageSceneDocument.RelativePath,
                    StageSceneDocument.Render(sceneJson, context),
                    [context.Application.Id]));
                artifacts.Add(PlannedArtifact.CreateText(
                    StageModuleRenderer.RelativePath,
                    StageModuleRenderer.Render(context),
                    [context.Application.Id]));
            }
        }

        var shared = request.Scope.Kind == ArtifactRenderScopeKind.Application
            ? (context.Application.Concepts, context.Application.Types)
            : SemanticSharedReferences.Collect(context, slices);
        artifacts.AddRange(shared.Concepts.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
        artifacts.AddRange(shared.Types.Select(_ => Artifact(SemanticCommonArtifactRenderer.Render(_, context))));
        artifacts.AddRange(SemanticEventSourceArtifactRenderer.Selected(context, slices).Select(source => Artifact(SemanticEventSourceArtifactRenderer.Render(source, context))));
        if (slices.Any(slice => slice.Slice.Commands.Any(command => command.Authorization is not null) ||
            slice.Slice.Queries.Any(query => query.Authorization is not null)))
        {
            artifacts.AddRange(SemanticPolicyArtifactRenderer.Render(context, slices).Select(Artifact));
            artifacts.AddRange(SemanticPolicyArtifactRenderer.RenderOpaque(context, slices).Select(Artifact));
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
        List<ArtifactRenderDiagnostic> diagnostics,
        ScreenPartitionsInput.Catalog? partitions)
    {
        if (request.Scope.Kind != ArtifactRenderScopeKind.Application)
        {
            return;
        }

        artifacts.AddRange(CratisScaffoldArtifacts.Render(request.Profile, context.RootNamespace, [context.Application.Id]));
        var scene = request.Profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        if (scene is null)
        {
            return;
        }

        var sceneJson = partitions is null
            ? System.Text.Encoding.UTF8.GetString(scene.Bytes.AsSpan())
            : ScreenPartitionsInput.Compose(scene, partitions);

        // Keep guarded Scene admission at the application boundary, where Scene artifacts are actually emitted.
        if (Contracts.Scene.SceneGuards.HasUnevaluableGuard(sceneJson))
        {
            var refusal = new Contracts.Scene.UnsupportedGuardedScreenAction("scene.json", Screenplay.Diagnostics.SourceLocation.Start);
            diagnostics.Add(Error(Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode, refusal.Message, request.Model.Application.Id));
            return;
        }

        artifacts.Add(PlannedArtifact.CreateText(SceneCompositionInput.RelativePath, sceneJson, [request.Model.Application.Id]));
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
