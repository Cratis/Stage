// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_validating_inventory : given.an_inventory
{
    [Fact] void should_count_semantic_owners_not_files_or_cascade_depth() => _inventory.Score.Counts.Total.ShouldEqual(4);
    [Fact] void should_count_all_degraded_owners() => _inventory.Score.Counts.Degraded.ShouldEqual(3);
    [Fact] void should_score_only_rendered_weight() => _inventory.Score.BasisPoints.ShouldEqual(2142);
    [Fact] void should_keep_the_unknown_owner_visible_and_conservative() => ArtifactRenderConstructWeights.For(ArtifactRenderConstructKind.Unknown).ShouldEqual(5);
    [Fact] void should_have_a_null_empty_denominator() => ArtifactRenderInventory.Create([]).Score.BasisPoints.ShouldBeNull();
    [Fact] void should_keep_a_two_hop_cascade() => _inventory.Constructs.Single(construct => construct.Identity == _leaf.Identity).Degradations.Single().Roots.Single().Owner.ShouldEqual(_middle.Identity);
    [Fact] void should_reject_default_inventory() => Reject(default);
    [Fact] void should_reject_duplicate_owners() => Reject([_root, _root]);
    [Fact] void should_reject_a_dangling_target_before_walking_another_chain() => Reject([_leaf, _middle]);
    [Fact] void should_reject_a_target_with_the_wrong_code() => Reject([_root, _middle with { Degradations = [_middle.Degradations.Single() with { Roots = [new(_root.Identity, "STAGE-DEGRADE-003")] }] }]);
    [Fact] void should_reject_a_cycle() => Reject([_middle with { Degradations = [_middle.Degradations.Single() with { Roots = [new(_leaf.Identity, "STAGE-DEGRADE-005")] }] }, _leaf]);
    [Fact] void should_reject_a_self_cycle() => Reject([_middle with { Degradations = [_middle.Degradations.Single() with { Roots = [new(_middle.Identity, "STAGE-DEGRADE-005")] }] }]);
    [Fact] void should_reject_unlinked_dependency_degradation() => Reject([_middle with { Degradations = [_middle.Degradations.Single() with { Roots = [] }] }]);
    [Fact] void should_reject_roots_on_an_independent_gap() => Reject([_root with { Degradations = [_root.Degradations.Single() with { Roots = [new(_root.Identity, "STAGE-DEGRADE-002")] }] }]);
    [Fact] void should_reject_rendered_owners_with_gaps() => Reject([_root with { Outcome = ArtifactRenderOutcome.Rendered }]);
    [Fact] void should_reject_degraded_owners_without_details() => Reject([_root with { Degradations = [] }]);
    [Fact] void should_reject_refused_owners_without_reasons() => Reject([_rendered with { Outcome = ArtifactRenderOutcome.Refused }]);
    [Fact] void should_reject_absolute_model_paths() => Reject([_rendered with { ModelPath = "/host/model.play" }]);
    [Fact] void should_reject_traversal_paths() => Reject([_rendered with { ModelPath = "../model.play" }]);
    [Fact] void should_reject_unknown_enum_values() => Reject([_rendered with { Kind = (ArtifactRenderConstructKind)100 }]);

    [Fact]
    void should_count_an_owner_once_when_it_has_independent_causes()
    {
        var second = Degraded(1, ArtifactRenderConstructKind.Command, "STAGE-DEGRADE-001", []).Degradations.Single();
        var inventory = ArtifactRenderInventory.Create([_root with { Degradations = [.. _root.Degradations, second] }, _rendered]);
        inventory.Score.Counts.Total.ShouldEqual(2);
        inventory.Score.TotalWeight.ShouldEqual(8L);
        inventory.Score.BasisPoints.ShouldEqual(3750);
    }

    [Fact]
    void should_preserve_inventory_through_strict_acceptance()
    {
        var strict = _plan.WithAcceptance(_plan.Degradations, strict: true);
        strict.Inventory.ShouldEqual(_inventory);
        strict.Inventory!.Score.ShouldEqual(_plan.Inventory!.Score);
        strict.CanPublishArtifacts.ShouldBeTrue();
        strict.Success.ShouldBeFalse();
        strict.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    }

    [Fact]
    void should_require_refusal_reasons_to_match_real_generation_errors()
    {
        var error = new ArtifactRenderDiagnostic("UNKNOWN-ERROR", ArtifactRenderDiagnosticSeverity.Error, "Unsafe generation.", _rendered.Identity);
        var refused = _rendered with { Outcome = ArtifactRenderOutcome.Refused, RefusalReasons = [error] };
        var inventory = ArtifactRenderInventory.Create([refused]);
        inventory.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        Plan(inventory).CanPublishArtifacts.ShouldBeFalse();
        Catch.Exception(() => ArtifactRenderPlan.Create(_request, [], []).WithInventory(inventory)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }

    [Fact]
    void should_classify_an_empty_selection_as_rendered() => ArtifactRenderInventory.Create([]).Outcome.ShouldEqual(ArtifactRenderOutcome.Rendered);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_legacy_outcome_and_digest_without_inventory(bool genuineError)
    {
        var diagnostic = _root.Degradations.Single().Evidence.Diagnostic with { Severity = genuineError ? ArtifactRenderDiagnosticSeverity.Error : ArtifactRenderDiagnosticSeverity.Warning };
        var legacy = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("app.cs", "// legacy payload")], [diagnostic]);
        legacy.Inventory.ShouldBeNull();
        legacy.Degradations.ShouldBeEmpty();
        legacy.Outcome.ShouldEqual(genuineError ? ArtifactRenderOutcome.Refused : ArtifactRenderOutcome.Complete);
        legacy.Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest(legacy.Target, legacy.TargetVersion, legacy.Renderer, legacy.RendererVersion, legacy.ApplicationName, legacy.Artifacts, legacy.Diagnostics));
    }

    void Reject(ImmutableArray<ArtifactRenderConstruct> constructs) => Catch.Exception(() => ArtifactRenderInventory.Create(constructs)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
