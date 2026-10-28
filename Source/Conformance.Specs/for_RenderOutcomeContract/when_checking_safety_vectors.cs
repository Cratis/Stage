// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

public class when_checking_safety_vectors : given.outcome_vectors
{
    // Exact reasons are reviewed vector inputs, never harvested from the plan being checked.
    [Theory]
    [InlineData("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", RenderOutcomeSafetyCategory.Trust)]
    [InlineData("STAGE-ESM-012", "Command 'Create' cannot render in generated C#: property name collision.", RenderOutcomeSafetyCategory.Integrity)]
    [InlineData("VECTOR-BUDGET", "The provider's model expansion limit was exceeded.", RenderOutcomeSafetyCategory.Budget)]
    [InlineData("STAGE-ESM-015", "Authorization of 'Find' contains a role claim URI, or has an unresolved or non-portable policy.", RenderOutcomeSafetyCategory.Authorization)]
    [InlineData("STAGE-ESM-006", "Produced event of command 'Create' cannot be rendered without changing its destination or mappings.", RenderOutcomeSafetyCategory.UnsafeEventRouting)]
    void should_accept_only_the_exact_reviewed_safety_reason(string code, string message, RenderOutcomeSafetyCategory category)
    {
        var error = new ArtifactRenderDiagnostic(code, ArtifactRenderDiagnosticSeverity.Error, message, Id(1));
        var construct = new ArtifactRenderConstruct(Id(1), ArtifactRenderConstructKind.Command, "model/outcomes.play", ArtifactRenderOutcome.Refused) { SemanticLocator = Locator(1), RefusalReasons = [error] };
        var plan = Plan([construct]);
        Assert.Empty(RenderOutcomeContract.Problems(plan, [new(code, message, category)]));
        Assert.Contains(RenderOutcomeContract.Problems(plan, [new(code, message + " different reason", category)]), problem => problem.StartsWith("Unclassified error:", StringComparison.Ordinal));
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.False(plan.Success);
        Assert.False(plan.CanPublishArtifacts);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, Assert.Single(plan.Diagnostics).Severity);
    }

    [Theory]
    [InlineData("STAGE-ESM-001")]
    [InlineData("STAGE-PLAN-999")]
    [InlineData("UNKNOWN")]
    void should_reject_non_safety_and_unknown_errors_without_downgrading_them(string code)
    {
        var owner = Degraded(1, "STAGE-DEGRADE-002");
        var error = new ArtifactRenderDiagnostic(code, ArtifactRenderDiagnosticSeverity.Error, "Unsupported semantic shape.", Id(1));
        var plan = ArtifactRenderPlan.Create(_request, [], [owner.Degradations[0].Evidence.Diagnostic, error]).WithInventory(ArtifactRenderInventory.Create([owner]));
        var problems = RenderOutcomeContract.Problems(plan, []);
        Assert.Contains($"Unclassified error: {code}: Unsupported semantic shape.", problems);
        Assert.Contains($"Missing refusal evidence: {code}.", problems);
        Assert.Contains(error, plan.Diagnostics);
        Assert.False(plan.Success);
    }

    [Fact]
    void should_reject_missing_inventory_precisely()
    {
        var plan = ArtifactRenderPlan.Create(_request, [], []);
        Assert.Equal("Missing semantic inventory.", Assert.Single(RenderOutcomeContract.Problems(plan, [])));
    }

    [Fact]
    void should_reject_an_unrecorded_strict_error_even_in_the_degradation_namespace()
    {
        var owner = new ArtifactRenderConstruct(Id(1), ArtifactRenderConstructKind.Query, "model/outcomes.play", ArtifactRenderOutcome.Rendered) { SemanticLocator = Locator(1) };
        var error = new ArtifactRenderDiagnostic("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Error, "Unrecorded gap.", Id(1));
        var plan = ArtifactRenderPlan.Create(_request, [], [error]).WithInventory(ArtifactRenderInventory.Create([owner])).WithAcceptance([], strict: true);
        var problems = RenderOutcomeContract.Problems(plan, []);
        Assert.Contains("Unrecorded degradation: STAGE-DEGRADE-002.", problems);
        Assert.Contains("Unclassified error: STAGE-DEGRADE-002: Unrecorded gap.", problems);
        Assert.False(plan.CanPublishArtifacts);
    }
}
