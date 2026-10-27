// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_applying_acceptance_with_removals : when_creating_with_required_removals.given_removals
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        var warning = new ArtifactRenderDiagnostic("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation described, not verified.", _model);
        var removal = new ArtifactRemoval("Orders/Totals/Fold.cs", Hash, _model);
        _original = ArtifactRenderPlan.Create(_request, _artifacts, [warning], [], [], [removal]);
        _strict = _original.WithAcceptance([new(warning, "Implement and verify the described requirement.")], strict: true);
    }

    [Fact] void should_reject_acceptance() => _strict.Success.ShouldBeFalse();
    [Fact] void should_keep_generation_safe() => _strict.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_keep_the_validated_removals() => _strict.RequiredRemovals.SequenceEqual(_original.RequiredRemovals).ShouldBeTrue();
    [Fact] void should_keep_the_validated_artifacts() => _strict.Artifacts.SequenceEqual(_original.Artifacts).ShouldBeTrue();
    [Fact] void should_keep_scope_metadata() => _strict.Scope.ShouldEqual(_original.Scope);
    [Fact] void should_keep_additional_scopes() => _strict.AdditionalScopes.SequenceEqual(_original.AdditionalScopes).ShouldBeTrue();
    [Fact] void should_keep_the_semantic_revision() => _strict.SemanticRevision.ShouldEqual(_original.SemanticRevision);
    [Fact] void should_keep_removals_after_reapplying_acceptance() => _strict.WithAcceptance(_strict.Degradations).RequiredRemovals.SequenceEqual(_original.RequiredRemovals).ShouldBeTrue();
}
