// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.when_creating;

public class with_a_graph_proven_omission : given.an_inventory_request
{
    ArtifactRenderDegradation _root = null!;
    ArtifactRenderDegradation _dependent = null!;

    void Establish()
    {
        _root = Evidence(_registration.Events.Single().Id, ArtifactRenderDegradationCodes.UnknownOwnerMember);
        _dependent = Evidence(_registration.Commands.Single().Id, ArtifactRenderDegradationCodes.DependencyDegradation);
        _causes[_dependent.Diagnostic.Artifact] = [_root.Diagnostic.Artifact];
        _plan = Plan([_root, _dependent], [new(_root.Diagnostic.Artifact, "event", [_root.Diagnostic.Code]), new(_dependent.Diagnostic.Artifact, "command", [_dependent.Diagnostic.Code])]);
    }

    void Because() => _inventory = CreateInventory();

    ArtifactRenderConstruct Root => _inventory.Constructs.Single(construct => construct.Identity == _root.Diagnostic.Artifact);
    ArtifactRenderConstruct Dependent => _inventory.Constructs.Single(construct => construct.Identity == _dependent.Diagnostic.Artifact);

    [Fact] void should_report_the_root_as_degraded() => Root.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_report_the_dependent_as_degraded() => Dependent.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_mark_the_root_as_graph_proven_omitted() => Root.Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.GraphProvenOmittedLeaf);
    [Fact] void should_mark_the_dependent_as_graph_proven_omitted() => Dependent.Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.GraphProvenOmittedLeaf);
    [Fact] void should_retain_the_original_root_evidence() => Root.Degradations.Single().Evidence.ShouldEqual(_root);
    [Fact] void should_retain_the_original_dependent_evidence() => Dependent.Degradations.Single().Evidence.ShouldEqual(_dependent);
    [Fact] void should_use_the_original_message_as_intent() => Dependent.Degradations.Single().Intent.ShouldEqual(_dependent.Diagnostic.Message);
    [Fact] void should_leave_the_root_without_causes() => Root.Degradations.Single().Roots.ShouldBeEmpty();
    [Fact] void should_reference_the_root_degradation() => Dependent.Degradations.Single().Roots.SequenceEqual([new ArtifactRenderGapReference(Root.Identity, _root.Diagnostic.Code)]).ShouldBeTrue();
    [Fact] void should_pass_plan_inventory_validation() => Catch.Exception(() => _plan.WithInventory(_inventory)).ShouldBeNull();
    [Fact] void should_carry_typed_causes_through_report_finalization() => CratisPlanReports.Finalize(_request, _plan, _options, _causes).Inventory!.Constructs.Single(construct => construct.Identity == Dependent.Identity).Degradations.Single().Roots.SequenceEqual(Dependent.Degradations.Single().Roots).ShouldBeTrue();
}
