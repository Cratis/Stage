// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Realization;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs;
using Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Plans deterministic Cratis application artifacts directly from the Screenplay executable semantic model.
/// </summary>
public sealed partial class CratisArtifactRenderPlanner : IArtifactRenderPlanner
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
        var plan = PlanCore(request, out var omissionAttempt);
        var warnings = ReleaseBaselineWarnings.For(request);

        plan = warnings.IsEmpty ? plan : CratisPlanDiagnosticReconstruction.Append(request, plan, warnings);
        return CratisArtifactRenderProfileAdmission.Matches(request, out var options, out _)
            ? FinalizeOmission(request, plan, options, omissionAttempt)
            : plan;
    }

    static ArtifactRenderPlan PlanCore(ArtifactRenderRequest request, out OwnerOmissionAttempt? omissionAttempt)
    {
        omissionAttempt = null;
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

        var diagnostics = new List<ArtifactRenderDiagnostic>();
        try
        {
            var result = PlanAdmitted(request, options, diagnostics);
            omissionAttempt = result.Attempt;
            return result.Plan;
        }
        catch (CSharpImportRejected exception)
        {
            diagnostics.Add(Error(CSharpImportRejected.Code, exception.Message, request.Model.Application.Id));
        }
        catch (InlineCodeRejected exception)
        {
            diagnostics.AddRange(exception.Diagnostics.Select(diagnostic => Error(diagnostic.Code, diagnostic.Message, request.Model.Application.Id)));
        }
        catch (InvalidTypedContext exception)
        {
            diagnostics.Add(Error("STAGE-ESM-021", exception.Message, request.Model.Application.Id));
        }
        catch (UnsupportedSemanticRendering exception)
        {
            diagnostics.Add(Error(exception.Code, exception.Message, request.Model.Application.Id));
        }
        catch (InvalidGeneratedIdentifier exception)
        {
            diagnostics.Add(Error(InvalidGeneratedIdentifier.DiagnosticCode, exception.Message, request.Model.Application.Id));
        }

        return CreatePlan(request, [], diagnostics);
    }

    static (ArtifactRenderPlan Plan, OwnerOmissionAttempt? Attempt) PlanAdmitted(ArtifactRenderRequest request, CratisRenderingOptions options, List<ArtifactRenderDiagnostic> diagnostics, OwnerOmissionAttempt? attempt = null)
    {
        var range = AdmitVersion(request, options);
        if (!range.IsAdmitted) return (CreatePlan(request, [], [.. range.Diagnostics]), null);
        diagnostics.AddRange(range.Diagnostics);

        var pass = new AdmittedPass(request, options, diagnostics, attempt);
        pass.Admit();
        pass.RenderDescriptors();
        pass.PrepareInterface();
        if (pass.Failed) return FailedPass(request, options, pass.Failure, attempt);

        pass.Refuse();
        pass.RenderRuntime();
        pass.RenderInterface();
        pass.RenderSemantics();
        pass.RenderApplicationScope();
        return CompletePass(pass.BuildPlan(), range, attempt);
    }

    // Only a pair inside the declared range is interpreted; every other unaudited pair is refused before any context is built.
    static CratisEsmRangeVerdict AdmitVersion(ArtifactRenderRequest request, CratisRenderingOptions options)
    {
        var standing = SemanticVersionFeatures.Supports(request.Model)
            ? EsmVersionStanding.Audited
            : CratisEsmSupport.Descriptor.Classify(request.Model.LanguageVersion, request.Model.SemanticVersion);
        return standing == EsmVersionStanding.NewerWithinRange || CratisEsmSupport.Descriptor.ScansMembers(request.Model.LanguageVersion, request.Model.SemanticVersion)
            ? CratisEsmRangeAdmission.Evaluate(request, new SemanticApplicationContext(request, options))
            : CratisEsmRangeAdmission.FromFindings(request, new(standing, []));
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

    // Graphs belong to the application: a scoped plan names the construct it leaves out instead of dropping it silently.
    static ImmutableArray<ArtifactRenderDiagnostic> GraphDiagnostics(ArtifactRenderRequest request, SemanticApplicationContext context)
    {
        if (!SemanticGraphAdmission.Declares(context.Application)) return [];
        return request.Scope.Kind == ArtifactRenderScopeKind.Application
            ? SemanticGraphAdmission.Check(context, ChronicleGraphSupport.PinnedClientAdmits)
            : [new(SemanticGraphAdmission.ScopedOut, ArtifactRenderDiagnosticSeverity.Information, "The application declares graphs, graph projections or graph specifications, which render with the application scope only; this scoped plan renders none of them.", context.Application.Id)];
    }

    static void AddScaffold(
        ArtifactRenderRequest request,
        SemanticApplicationContext context,
        List<PlannedArtifact> artifacts)
    {
        if (request.Scope.Kind != ArtifactRenderScopeKind.Application)
        {
            return;
        }

        artifacts.AddRange(CratisScaffoldArtifacts.Render(request.Profile, context.RootNamespace, [context.Application.Id], request.Model.Application.Name, SemanticLifecycleArtifactRenderer.HasEventTriggers(context), context.EventContracts.Any && !context.EventContracts.Blockers(context).Any() ? context.ContractsFolder : null, SemanticGraphAdmission.Declares(context.Application)));
    }

    static string? AdmittedScene(ArtifactRenderRequest request, ScreenPartitionsInput.Catalog? partitions, IReadOnlyList<LocatedSemanticSlice>? userInterface, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // Only plans that emit shared Scene artifacts admit the Scene; other scoped plans must not be refused for it.
        if (userInterface is null) return null;
        var scene = request.Profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        if (scene is null)
        {
            return null;
        }

        // Partitioned screens are composed from the slices whose UI is present; an unpartitioned Scene is the
        // caller's whole composition and is carried as authored.
        var sceneJson = partitions is null
            ? System.Text.Encoding.UTF8.GetString(scene.Bytes.AsSpan())
            : ScreenPartitionsInput.Compose(scene, partitions, userInterface.Select(slice => slice.Slice.Id.ToString()).ToHashSet(StringComparer.Ordinal));

        // Admission runs before rendering, even if another independent gate has already refused.
        var guardDiagnostics = Contracts.Scene.SceneGuards.GuardAdmissionDiagnostics(sceneJson);
        if (guardDiagnostics.Count != 0)
        {
            // Preserve the existing action diagnostic when both surfaces are refused.
            var code = guardDiagnostics.Contains(Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode)
                ? Contracts.Scene.UnsupportedGuardedScreenAction.DiagnosticCode
                : Contracts.Scene.UnsupportedGuardedInteraction.DiagnosticCode;
            var location = Screenplay.Diagnostics.SourceLocation.Start;
            Exception refusal = code == Contracts.Scene.UnsupportedGuardedInteraction.DiagnosticCode
                ? new Contracts.Scene.UnsupportedGuardedInteraction("scene.json", location)
                : new Contracts.Scene.UnsupportedGuardedScreenAction("scene.json", location);
            diagnostics.Add(Error(code, refusal.Message, request.Model.Application.Id));
        }

        return sceneJson;
    }

    static IEnumerable<PlannedArtifact> RelocateSourceLines(ArtifactRenderProfile profile, IEnumerable<PlannedArtifact> artifacts) =>
        SourceLineMappingInput.TryRead(profile, out var mapping) && mapping is not null
            ? SourceLineDirectives.Relocate(profile, mapping, artifacts)
            : artifacts;

    static SemanticOwnerOmission OmissionFor(OwnerOmissionAttempt? attempt) => attempt?.Omission ?? SemanticOwnerOmission.None;

    static (ImmutableArray<ArtifactRenderDiagnostic> Warnings, ImmutableArray<ArtifactRenderDegradation> Degradations,
        ImmutableArray<ArtifactRenderRefusal> Refusals) OmissionEvidence(OwnerOmissionAttempt? attempt) => attempt?.Evidence ?? ([], [], []);

    static bool PassFailed(List<ArtifactRenderDiagnostic> diagnostics, ImmutableArray<ArtifactRenderRefusal> refusals, OwnerOmissionAttempt? attempt) =>
        diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error) && (refusals.IsEmpty || attempt is not null);

    static (ArtifactRenderPlan Plan, OwnerOmissionAttempt? Attempt) FailedPass(ArtifactRenderRequest request, CratisRenderingOptions options, OwnerOmissionFailure failure, OwnerOmissionAttempt? attempt)
    {
        var refused = CreatePlan(request, [], failure.Diagnostics);
        if (attempt is not null || options.Strict) return (refused, null);
        try
        {
            return RetryOmission(request, options, failure, refused);
        }
        catch (Exception)
        {
            return (refused, null);
        }
    }

    static (ArtifactRenderPlan Plan, OwnerOmissionAttempt? Attempt) RetryOmission(ArtifactRenderRequest request, CratisRenderingOptions options, OwnerOmissionFailure failure, ArtifactRenderPlan refused)
    {
        if (!failure.Diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && failure.Context.OwnerScopedRefusals.Contains(diagnostic))) return (refused, null);
        var index = new SemanticDependencyIndex(failure.Context);
        var roots = SemanticOwnerScopedDegradation.TryRoots(failure.Context, failure.Diagnostics, index, new(false, failure.Removals));
        if (roots is null || SemanticOwnerOmission.Compute(failure.Context, index, roots) is not { } omission) return (refused, null);
        if (!HasCompleteCauses(omission)) return (refused, null);
        var attempt = new OwnerOmissionAttempt(omission, failure.Context, refused);
        var result = PlanAdmitted(request, options, [], attempt);
        return result.Plan.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error) ? (refused, null) : result;
    }

    static bool HasCompleteCauses(SemanticOwnerOmission omission) =>
        omission.OrderedClosure.All(owner => omission.Causes.TryGetValue(owner, out var roots) && !roots.IsEmpty);

    static (ArtifactRenderPlan Plan, OwnerOmissionAttempt? Attempt) CompletePass(ArtifactRenderPlan plan, CratisEsmRangeVerdict range, OwnerOmissionAttempt? attempt)
    {
        if (attempt is not null)
        {
            if (SemanticOwnerOmission.Overlaps(plan.Artifacts, attempt.Omission.Closure).Count > 0)
                throw new InvalidArtifactRenderContract("An artifact realizes an omitted semantic owner.");
            plan = plan.WithAcceptance([.. plan.Degradations, .. attempt.Evidence.Degradations]);
        }
        return (range.Evidence.IsEmpty ? plan : plan.WithPlanEvidence(range.Evidence), attempt);
    }

    static ArtifactRenderPlan FinalizeOmission(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options, OwnerOmissionAttempt? attempt)
    {
        try
        {
            return CratisPlanReports.Finalize(request, CratisPlanAcceptance.Apply(request, plan, options), options, attempt?.Omission.Causes);
        }
        catch (InvalidArtifactRenderContract) when (attempt is not null)
        {
            var warnings = ReleaseBaselineWarnings.For(request);
            var refused = warnings.IsEmpty ? attempt.Fallback : CratisPlanDiagnosticReconstruction.Append(request, attempt.Fallback, warnings);
            return CratisPlanReports.Finalize(request, CratisPlanAcceptance.Apply(request, refused, options), options);
        }
    }

    static void AddSpecificationRefusalSummary(ArtifactRenderRequest request, List<ArtifactRenderDiagnostic> diagnostics, ImmutableArray<ArtifactRenderRefusal> refusals)
    {
        var specifications = refusals.Count(refusal => refusal.Kind == "specification" && refusal.Codes.All(code => !code.StartsWith("STAGE-DEGRADE-", StringComparison.Ordinal)));
        if (specifications == 0) return;
        diagnostics.Add(new("STAGE-PLAN-007", ArtifactRenderDiagnosticSeverity.Information, $"Rendered around {specifications} refused specifications", request.Model.Application.Id));
    }

    static PlannedArtifact Artifact(RenderedFile file) => PlannedArtifact.CreateText(file.RelativePath, file.Content, file.Sources);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);

    sealed record OwnerOmissionFailure(SemanticApplicationContext Context, List<ArtifactRenderDiagnostic> Diagnostics, ImmutableArray<ArtifactRemoval> Removals);

    sealed record OwnerOmissionAttempt(SemanticOwnerOmission Omission, SemanticApplicationContext Context, ArtifactRenderPlan Fallback)
    {
        internal (ImmutableArray<ArtifactRenderDiagnostic> Warnings, ImmutableArray<ArtifactRenderDegradation> Degradations,
            ImmutableArray<ArtifactRenderRefusal> Refusals) Evidence
        { get; } = SemanticOwnerScopedDegradation.Evidence(Context, Omission);
    }
}
