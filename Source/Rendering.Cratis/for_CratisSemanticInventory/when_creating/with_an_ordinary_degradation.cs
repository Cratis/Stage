// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.when_creating;

public class with_an_ordinary_degradation : given.an_inventory_request
{
    ArtifactRenderDegradation _evidence = null!;

    void Establish()
    {
        _evidence = Evidence(_registration.Commands.Single().Id, ArtifactRenderDegradationCodes.UnsupportedConstruct);
        _plan = Plan([_evidence], []);
    }

    void Because() => _inventory = CreateInventory();

    ArtifactRenderConstruct Construct => _inventory.Constructs.Single(construct => construct.Identity == _evidence.Diagnostic.Artifact);

    [Fact] void should_keep_the_throwing_body_placeholder() => Construct.Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.ThrowingOperationBody);
    [Fact] void should_not_add_causes() => Construct.Degradations.Single().Roots.ShouldBeEmpty();
    [Fact] void should_pass_plan_inventory_validation() => Catch.Exception(() => _plan.WithInventory(_inventory)).ShouldBeNull();
}
