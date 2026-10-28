// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.when_creating;

public class with_a_skipped_specification : given.an_inventory_request
{
    ArtifactRenderDiagnostic _warning = null!;

    void Establish()
    {
        var specification = _registration.Specifications.Single().Id;
        _warning = new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Warning, "Omitted specification 'RegisteringProject'.", specification);
        _plan = ArtifactRenderPlan.Create(_request, [], [_warning], [], [new(specification, "specification", [_warning.Code])]);
    }

    void Because() => _inventory = CreateInventory();

    ArtifactRenderConstruct Omitted => _inventory.Constructs.Single(construct => construct.Identity == _warning.Artifact);

    [Fact] void should_keep_the_refused_outcome() => Omitted.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_keep_the_original_warning_reason() => Omitted.RefusalReasons.SequenceEqual([_warning]).ShouldBeTrue();
    [Fact] void should_not_add_degradation_gaps() => Omitted.Degradations.ShouldBeEmpty();
    [Fact] void should_pass_plan_inventory_validation() => Catch.Exception(() => _plan.WithInventory(_inventory)).ShouldBeNull();
}
