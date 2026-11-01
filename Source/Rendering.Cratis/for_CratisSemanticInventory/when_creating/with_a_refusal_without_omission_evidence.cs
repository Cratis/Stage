// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.when_creating;

public class with_a_refusal_without_omission_evidence : given.an_inventory_request
{
    Exception? _error;

    void Establish()
    {
        var evidence = Evidence(_registration.Commands.Single().Id, ArtifactRenderDegradationCodes.UnsupportedConstruct);
        _plan = Plan([evidence], [new(evidence.Diagnostic.Artifact, "command", [evidence.Diagnostic.Code])]);
    }

    void Because() => _error = Catch.Exception(() => CreateInventory());

    [Fact] void should_not_treat_an_unrelated_degradation_as_omission_proof() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
