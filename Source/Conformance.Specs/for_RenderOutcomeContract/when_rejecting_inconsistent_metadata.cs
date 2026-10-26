// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

public class when_rejecting_inconsistent_metadata : given.outcome_vectors
{
    [Fact]
    void should_reject_a_dependency_without_its_root()
    {
        var dangling = Degraded(2, "STAGE-DEGRADE-005", [new(Id(1), "STAGE-DEGRADE-002")]);
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([dangling]));
        Assert.Contains("missing", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    void should_reject_inventory_evidence_not_emitted_by_the_plan()
    {
        var plan = ArtifactRenderPlan.Create(_request, [], []);
        var inventory = ArtifactRenderInventory.Create([Degraded(1, "STAGE-DEGRADE-002")]);
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => plan.WithInventory(inventory));
        Assert.Equal("Degradation evidence does not match an emitted warning.", exception.Message);
    }

    [Fact]
    void should_reject_strict_metadata_that_hides_an_inventory_gap()
    {
        var plan = Plan([Degraded(1, "STAGE-DEGRADE-002")]);
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => plan.WithAcceptance([], strict: true));
        Assert.Equal("Acceptance evidence must agree with the attached inventory.", exception.Message);
    }

    [Fact]
    void should_reject_refusal_metadata_without_its_original_error()
    {
        var error = new ArtifactRenderDiagnostic("STAGE-ESM-016", ArtifactRenderDiagnosticSeverity.Error, "Unaudited input.", Id(1));
        var construct = new ArtifactRenderConstruct(Id(1), ArtifactRenderConstructKind.Command, "model/outcomes.play", ArtifactRenderOutcome.Refused) { SemanticLocator = Locator(1), RefusalReasons = [error] };
        var plan = ArtifactRenderPlan.Create(_request, [], []);
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => plan.WithInventory(ArtifactRenderInventory.Create([construct])));
        Assert.Equal("Inventory refusal evidence does not match an original generation error.", exception.Message);
    }
}
