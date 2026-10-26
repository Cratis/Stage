// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Renderers.for_LegacyGeneratedTypes;

public class when_rendering_concepts_and_validators : concepts
{
    IReadOnlyList<RenderedFile> _files = null!;

    void Because() => _files = [.. _applicationSet.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, _applicationSet, "AcmeBilling"))];

    [Fact] void should_inventory_every_emitted_declaration() =>
        InventoryAssertions.Missing(_files, _applicationSet, "AcmeBilling").ShouldBeEmpty();
    [Fact] void should_inventory_the_concept_validator() =>
        LegacyGeneratedTypes.For(_applicationSet, "AcmeBilling").Any(type => type.Name == "DiscountPercentageValidator").ShouldBeTrue();
}
