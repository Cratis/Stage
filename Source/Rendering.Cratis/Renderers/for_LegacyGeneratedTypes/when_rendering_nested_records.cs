// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Renderers.for_LegacyGeneratedTypes;

public class when_rendering_nested_records : a_projection_with_nested_blocks
{
    RenderedFile _file = null!;

    void Because() => _file = new StateViewSliceRenderer().Render(_detailsSlice, _applicationSet, "AcmeBilling");

    [Fact] void should_inventory_every_emitted_declaration() =>
        InventoryAssertions.Missing([_file], _applicationSet, "AcmeBilling").ShouldBeEmpty();
    [Fact] void should_inventory_the_deeply_nested_record() =>
        LegacyGeneratedTypes.For(_applicationSet, "AcmeBilling").Any(type => type.Name == "InvoiceDetailsShippingCarrier").ShouldBeTrue();
}
