// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

/// <summary>Checks outcome evidence without rewriting the plan, diagnostics, output or acceptance.</summary>
public static class RenderOutcomeContract
{
    /// <summary>
    /// Finds contract violations using an explicit reviewed code/reason safety table.
    /// This checker is reusable by corpus migration; it does not classify legacy errors by prefix.
    /// </summary>
    /// <param name="plan">The original plan under verification.</param>
    /// <param name="safetyCases">Exact reviewed safety dispositions, not derived from observed errors.</param>
    /// <returns>Deterministic violation descriptions; empty means the contract is satisfied.</returns>
    public static ImmutableArray<string> Problems(ArtifactRenderPlan plan, ImmutableArray<RenderOutcomeSafetyCase> safetyCases)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var problems = new List<string>();
        if (plan.Inventory is not { } inventory) return ["Missing semantic inventory."];
        CheckDiagnostics(plan, safetyCases, problems);
        CheckConstructs(plan, inventory, safetyCases, problems);
        return [.. problems.Order(StringComparer.Ordinal)];
    }

    static void CheckDiagnostics(ArtifactRenderPlan plan, ImmutableArray<RenderOutcomeSafetyCase> safetyCases, List<string> problems)
    {
        foreach (var diagnostic in plan.Diagnostics) CheckDiagnostic(plan, diagnostic, safetyCases, problems);
        if (plan.Strict && plan.Diagnostics.Any(diagnostic => IsUnpromoted(plan, diagnostic)))
            problems.Add("Strict degradation was not promoted.");
    }

    static void CheckDiagnostic(ArtifactRenderPlan plan, ArtifactRenderDiagnostic diagnostic, ImmutableArray<RenderOutcomeSafetyCase> safetyCases, List<string> problems)
    {
        var degradation = IsRecorded(plan, diagnostic);
        if (diagnostic.Code.StartsWith("STAGE-DEGRADE-", StringComparison.Ordinal) && !degradation)
            problems.Add($"Unrecorded degradation: {diagnostic.Code}.");
        if (diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Error) return;
        if (plan.Strict && degradation) return;
        if (!IsSafety(diagnostic, safetyCases))
            problems.Add($"Unclassified error: {diagnostic.Code}: {diagnostic.Message}");
        if (!plan.Inventory!.Constructs.Any(construct => construct.RefusalReasons.Contains(diagnostic)))
            problems.Add($"Missing refusal evidence: {diagnostic.Code}.");
    }

    static bool IsUnpromoted(ArtifactRenderPlan plan, ArtifactRenderDiagnostic diagnostic) =>
        diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && IsRecorded(plan, diagnostic);

    static bool IsRecorded(ArtifactRenderPlan plan, ArtifactRenderDiagnostic diagnostic) =>
        plan.Degradations.Any(gap => gap.Diagnostic == diagnostic with { Severity = ArtifactRenderDiagnosticSeverity.Warning });

    static void CheckConstructs(ArtifactRenderPlan plan, ArtifactRenderInventory inventory, ImmutableArray<RenderOutcomeSafetyCase> safetyCases, List<string> problems)
    {
        foreach (var construct in inventory.Constructs)
        {
            foreach (var reason in construct.RefusalReasons)
            {
                if (!IsSafety(reason, safetyCases))
                    problems.Add($"Unclassified refusal: {reason.Code}: {reason.Message}");
            }
            if (construct.Degradations.Any(gap => gap.PlaceholderKind != ArtifactRenderPlaceholderKind.GraphProvenOmittedLeaf) &&
                !plan.Artifacts.Any(artifact => artifact.Sources.Contains(construct.Identity)))
            {
                problems.Add($"Missing retained placeholder: {construct.Identity}.");
            }
        }
    }

    static bool IsSafety(ArtifactRenderDiagnostic diagnostic, ImmutableArray<RenderOutcomeSafetyCase> safetyCases) =>
        safetyCases.Any(safety => Enum.IsDefined(safety.Category) && safety.Code == diagnostic.Code && safety.Message == diagnostic.Message);
}
