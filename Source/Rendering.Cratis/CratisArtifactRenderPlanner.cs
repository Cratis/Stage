// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

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

        return warnings.IsEmpty ? plan : ArtifactRenderPlan.Create(request, plan.Artifacts, [.. plan.Diagnostics, .. warnings], plan.ImplementationGaps, plan.Refusals, plan.RequiredRemovals);
    }

    static ArtifactRenderPlan PlanCore(ArtifactRenderRequest request)
    {
        if (!CratisArtifactRenderProfileAdmission.Matches(request, out var options, out var mismatch))
        {
            // Without exact profile options, later admission gates have no target to reason about.
            return ArtifactRenderPlan.Create(
                request,
                [],
                [Error(
                    "STAGE-CRATIS-001",
                    $"The artifact render profile is not the exact package-owned Cratis profile. {mismatch}",
                    request.Model.Application.Id)]);
        }

        return PlanAdmitted(request, options, []);
    }

    static ArtifactRenderPlan PlanAdmitted(ArtifactRenderRequest request, CratisRenderingOptions options, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var artifacts = new List<PlannedArtifact>();
        if (!SemanticVersionFeatures.Supports(request.Model))
        {
            // No audited semantic surface means later gates cannot interpret the model meaningfully.
            return CreatePlan(request, [], [Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", request.Model.Application.Id)]);
        }

        var context = new SemanticApplicationContext(request, options);
        context.ScopeFlatProjections();
        var slices = context.SelectedSlices();
        diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, slices));
        diagnostics.AddRange(SemanticImplementationAdmission.Verify(request));
        diagnostics.AddRange(SemanticTypedContextAdmission.Verify(request, out var descriptors));
        diagnostics.AddRange(SemanticEventLineageAdmission.Verify(request));

        // Dependent body checks must not dereference rejected sidecar entries. Keep their refusals,
        // but expose only unique requirements and admitted descriptors to those checks.
        var admittedRequest = request with
        {
            ImplementationRequirements = request.ImplementationRequirements.IsDefault ? [] :
                [.. request.ImplementationRequirements.Where(requirement => requirement is not null)
                    .GroupBy(requirement => requirement.RequirementId, StringComparer.Ordinal)
                    .Where(group => group.Count() == 1).Select(group => group.Single())],
            TypedContextDescriptors = descriptors
        };
        context = new SemanticApplicationContext(admittedRequest, options);
        context.ScopeFlatProjections();
        slices = context.SelectedSlices();
        SemanticCratisAdmission.Evaluate(context, slices, diagnostics);
        var removals = SemanticCratisAdmission.AdmitReducerMigration(context, diagnostics);
        var parts = new RenderingPartAdmission(context, diagnostics);
        var selectedReducers = slices.SelectMany(_ => _.Slice.Reducers)
            .SelectMany(reducer => reducer.Transitions.Select(transition => (transition.RequirementId, reducer.ReadModel)))
            .ToHashSet();
        foreach (var descriptor in descriptors)
        {
            var owner = descriptor.OperationId ?? context.Application.Id;
            var wrappers = parts.Render(owner, () => [SemanticTypedContextRenderer.Render(descriptor, context)]);
            if (selectedReducers.Contains((descriptor.RequirementId, owner))) artifacts.AddRange(wrappers.Select(Artifact));
        }

        ScreenPartitionsInput.TryRead(request.Profile, request.Model, out var partitions, out _);
        var sceneJson = AdmittedScene(request, partitions, diagnostics);
        var refusals = options.Refusals == ArtifactRefusalMode.RenderAround ? SemanticRefusalScope.Classify(context, diagnostics) : [];
        parts.AdmitDiagnostics(refusals, removals);
        context.RefusedSpecifications = [.. refusals.Select(refusal => refusal.Artifact)];

        var validations = SemanticValidationContextRuntime.Selected(context, slices)
            .Where(descriptor => !parts.IsOmitted(descriptor.OperationId ?? context.Application.Id))
            .ToArray();
        foreach (var descriptor in validations)
        {
            artifacts.AddRange(parts.Render(
                descriptor.OperationId ?? context.Application.Id,
                () => [SemanticValidationContextRuntime.RenderWrapper(context, descriptor)]).Select(Artifact));
        }
        if (validations.Length > 0)
        {
            artifacts.Add(Artifact(SemanticValidationContextRuntime.RenderBodies(context, validations.Where(descriptor => !parts.IsOmitted(descriptor.OperationId ?? context.Application.Id)).Select(descriptor =>
                (descriptor, context.Request.ImplementationContents[descriptor.RequirementId])))));
        }

        if (GeneratedTypeNames.UsesCommandReceiptTime(slices.SelectMany(located => located.Slice.Commands)))
        {
            artifacts.Add(Artifact(CommandReceiptTimeRendering.Render(context.RootNamespace)));
        }

        if (GeneratedTypeNames.UsesProtectedReads(slices.SelectMany(located => located.Slice.Commands)))
        {
            artifacts.Add(Artifact(SemanticProtectedReadAdmissionRenderer.Render(context, slices.SelectMany(located => located.Slice.Commands))));
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

        artifacts.AddRange(parts.RenderModelParts(slices).Select(Artifact));
        AddScaffold(request, context, artifacts, sceneJson);

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
                : DefaultSceneComposition.Create(parts.SceneContext(options));
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
                var documentJson = composed is not null
                    ? CanonicalSceneJson.Serialize(composed)
                    : System.Text.Encoding.UTF8.GetString(artifacts.Single(_ => _.RelativePath == SceneCompositionInput.RelativePath).Bytes.AsSpan());
                artifacts.Add(PlannedArtifact.CreateText(
                    StageSceneDocument.RelativePath,
                    StageSceneDocument.Render(documentJson, context),
                    [context.Application.Id]));
                artifacts.Add(PlannedArtifact.CreateText(
                    StageModuleRenderer.RelativePath,
                    StageModuleRenderer.Render(context),
                    [context.Application.Id]));
            }
        }

        return CreatePlan(request, artifacts, diagnostics, parts);
    }

    static (List<PlannedArtifact> Artifacts, ImmutableArray<ArtifactImplementationGap> Gaps) AddPerformerGaps(ArtifactRenderRequest request, IEnumerable<PlannedArtifact> artifacts, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var planned = artifacts.ToList();
        if (planned.Count == 0 || !diagnostics.Exists(diagnostic => diagnostic.Code == SemanticImplementationAdmission.PerformerGapCode)) return (planned, []);
        if (!CratisArtifactRenderProfileAdmission.Matches(request, out var options, out _)) return (planned, []);
        var gaps = PerformerGaps(new(request, options), planned, diagnostics);
        if (gaps.IsEmpty) return (planned, gaps);
        var entries = gaps.Select(gap => $"- Query requirement `{gap.RequirementId}` ({gap.Owner}): {string.Join(' ', gap.Hints)}");
        planned.Add(PlannedArtifact.CreateText(
            "STAGE-NOT-RUNNABLE.md",
            $"# Not fully runnable\n\nThis render contains unverified performer gaps. Calls to these queries throw NotImplementedException.\n\n{string.Join('\n', entries)}\n",
            [request.Model.Application.Id]));
        return (planned, gaps);
    }

    static ImmutableArray<ArtifactImplementationGap> PerformerGaps(SemanticApplicationContext context, List<PlannedArtifact> artifacts, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var gaps = ImmutableArray.CreateBuilder<ArtifactImplementationGap>();
        foreach (var diagnostic in diagnostics.Where(diagnostic => diagnostic.Code == SemanticImplementationAdmission.PerformerGapCode))
        {
            if (!context.Queries.TryGetValue(diagnostic.Artifact, out var query)) continue;

            // Policy artifacts also list the query as a source; only the slice artifact declares the performer method.
            var gap = artifacts
                .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.Sources.Contains(diagnostic.Artifact))
                .Select(artifact => SemanticQueryContextRuntime.Gap(context, query, artifact, diagnostic.Message))
                .FirstOrDefault(candidate => candidate is not null);
            if (gap is not null) gaps.Add(gap);
        }
        return [.. gaps.OrderBy(gap => gap.RequirementId, StringComparer.Ordinal)];
    }

    static void AddScaffold(
        ArtifactRenderRequest request,
        SemanticApplicationContext context,
        List<PlannedArtifact> artifacts,
        string? sceneJson)
    {
        if (request.Scope.Kind != ArtifactRenderScopeKind.Application)
        {
            return;
        }

        artifacts.AddRange(CratisScaffoldArtifacts.Render(request.Profile, context.RootNamespace, [context.Application.Id]));
        if (sceneJson is not null)
        {
            artifacts.Add(PlannedArtifact.CreateText(SceneCompositionInput.RelativePath, sceneJson, [request.Model.Application.Id]));
        }
    }

    static string? AdmittedScene(ArtifactRenderRequest request, ScreenPartitionsInput.Catalog? partitions, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // Only application plans emit shared Scene artifacts; scoped plans must not be refused for them.
        if (request.Scope.Kind != ArtifactRenderScopeKind.Application) return null;
        var scene = request.Profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        if (scene is null)
        {
            return null;
        }

        var sceneJson = partitions is null
            ? System.Text.Encoding.UTF8.GetString(scene.Bytes.AsSpan())
            : ScreenPartitionsInput.Compose(scene, partitions);

        // Admission runs before rendering, even if another independent gate has already refused.
        if (Contracts.Scene.SceneGuards.HasUnevaluableGuard(sceneJson))
        {
            var refusal = new Contracts.Scene.UnsupportedGuardedScreenAction("scene.json", Screenplay.Diagnostics.SourceLocation.Start);
            diagnostics.Add(Error(Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode, refusal.Message, request.Model.Application.Id));
        }

        return sceneJson;
    }

    static void DeduplicateRequirementDiagnostics(List<ArtifactRenderDiagnostic> diagnostics)
    {
        // Envelope verification and use-site admission report the same rejected requirement.
        // Retain the envelope owner's diagnostic, after both owners have been omitted.
        var reported = new HashSet<string>(StringComparer.Ordinal);
        diagnostics.RemoveAll(diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-020", StringComparison.Ordinal) && !reported.Add(diagnostic.Message));
    }

    static ArtifactRenderPlan CreatePlan(
        ArtifactRenderRequest request,
        IEnumerable<PlannedArtifact> artifacts,
        List<ArtifactRenderDiagnostic> diagnostics,
        RenderingPartAdmission? parts = null)
    {
        if (parts is null) return ArtifactRenderPlan.Create(request, [.. artifacts], [.. diagnostics]);
        var refusals = parts.Refusals;
        var removals = parts.Removals;
        var original = artifacts.ToList();
        var admitted = parts.Retained(ArtifactPathAdmission.Admit(original, request.Model.Application.Id, diagnostics));
        original.AddRange(parts.MissingDeclarations());
        admitted = ArtifactDependencyAdmission.Admit(original, admitted, request.Model.Application, diagnostics);
        DeduplicateRequirementDiagnostics(diagnostics);
        OmittedSceneArtifacts.Repair(request, original, admitted);
        var (planned, gaps) = AddPerformerGaps(request, admitted, diagnostics);
        var renderingGaps = RenderingGapReports.FromDiagnostics(diagnostics.Where(diagnostic =>
            RenderingGapReports.IsPathDiagnostic(diagnostic) || parts.IsGap(diagnostic)));
        gaps = gaps.AddRange(renderingGaps);
        if (!renderingGaps.IsEmpty) RenderingGapReports.AddMarker(planned, gaps, request.Model.Application.Id);

        // Successful diagnostics (and their digests) are unchanged. The contract owns sorting.
        if (diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            diagnostics = [.. diagnostics.DistinctBy(diagnostic => (diagnostic.Code, diagnostic.Artifact, diagnostic.Message))];
        }

        if (!refusals.IsEmpty)
        {
            diagnostics.Add(new(
                "STAGE-PLAN-007",
                ArtifactRenderDiagnosticSeverity.Information,
                $"Rendered around {refusals.Length} refused specifications",
                request.Model.Application.Id));
        }

        // A plan carrying errors never removes files; publication then refuses the leftover classic reducer.
        var required = removals.IsDefault || diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error) ? [] : removals;
        return ArtifactRenderPlan.Create(request, [.. planned], [.. diagnostics], gaps, refusals, required);
    }

    static PlannedArtifact Artifact(RenderedFile file) => PlannedArtifact.CreateText(file.RelativePath, file.Content, file.Sources);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
