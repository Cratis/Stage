// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_acceptance_or_inventory_is_applied : given.a_provenance_request
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _accepted = null!;
    ArtifactRenderPlan _evidenced = null!;
    ArtifactRenderPlan _inventoried = null!;

    void Because()
    {
        var warning = new ArtifactRenderDiagnostic("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation is unverified.", _request.Model.Application.Id);
        var request = With([Input("kit", "1", "one")], [Requirement("r1")]);
        _original = ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("same.cs", "same")], [warning]).WithInputComponents([new(ArtifactRenderComponentKind.Kit, "acme", "1", null)]);
        _accepted = _original.WithAcceptance([new(warning, "Verify it.")], strict: true);
        _evidenced = _original.WithPlanEvidence([new(warning, "Verify it.")]);
        _inventoried = _original.WithInventory(ArtifactRenderInventory.Create([]));
    }

    [Fact] void should_retain_it_through_acceptance() => _accepted.InputProvenance.ShouldEqual(_original.InputProvenance);
    [Fact] void should_retain_it_through_plan_evidence() => _evidenced.InputProvenance.ShouldEqual(_original.InputProvenance);
    [Fact] void should_retain_it_through_an_inventory() => _inventoried.InputProvenance.ShouldEqual(_original.InputProvenance);
    [Fact] void should_have_components_to_retain() => _original.InputProvenance.Components.Length.ShouldEqual(1);
}
