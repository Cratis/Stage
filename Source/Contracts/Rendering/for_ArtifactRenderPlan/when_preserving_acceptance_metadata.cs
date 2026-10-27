// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_preserving_acceptance_metadata : for_ArtifactImplementationGap.given.a_gap
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        var request = _request;
        var warning = new ArtifactRenderDiagnostic("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation is described.", _request.Model.Application.Id);
        _original = ArtifactRenderPlan.Create(request, _artifacts, [warning], [_gap], [new(_request.Model.Application.Id, "specification", ["STAGE-ESM-011"])]);
        _strict = _original.WithAcceptance([new(warning, "Implement and verify the described requirement.")], strict: true);
    }

    [Fact] void should_preserve_nonempty_implementation_gaps() => _strict.ImplementationGaps.Single().ShouldEqual(_original.ImplementationGaps.Single());
    [Fact] void should_preserve_nonempty_refusals() => _strict.Refusals.Single().ShouldEqual(_original.Refusals.Single());
    [Fact] void should_preserve_the_primary_scope() => _strict.Scope.ShouldEqual(_request.Scope);
    [Fact] void should_preserve_evidence_on_reapplication() => _strict.WithAcceptance(_strict.Degradations).Degradations.Single().ShouldEqual(_strict.Degradations.Single());
}
