// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_with_required_removals;

public class with_invalid_removals : given_removals
{
    [Fact] void should_reject_a_removal_colliding_with_an_artifact() =>
        Catch.Exception(() => Create(new ArtifactRemoval("orders/totals/TOTALS.cs", Hash, _model))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_a_malformed_hash() =>
        Catch.Exception(() => Create(new ArtifactRemoval("Orders/Totals/Fold.cs", Hash.ToUpperInvariant(), _model))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_an_unset_model() =>
        Catch.Exception(() => Create(new ArtifactRemoval("Orders/Totals/Fold.cs", Hash, default))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_a_traversing_path() =>
        Catch.Exception(() => Create(new ArtifactRemoval("../Fold.cs", Hash, _model))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_case_colliding_removals() =>
        Catch.Exception(() => Create(new ArtifactRemoval("Orders/Fold.cs", Hash, _model), new ArtifactRemoval("orders/fold.cs", Hash, _model))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_a_removal_whose_model_has_no_artifact() =>
        Catch.Exception(() => Create(new ArtifactRemoval("Orders/Totals/Fold.cs", Hash, SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("Other")))))).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_reject_removals_on_a_plan_with_errors() =>
        Catch.Exception(() => ArtifactRenderPlan.Create(_request, _artifacts, [new("STAGE-X", ArtifactRenderDiagnosticSeverity.Error, "failed", _model)], [], [], [new("Orders/Totals/Fold.cs", Hash, _model)]))
            .ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
