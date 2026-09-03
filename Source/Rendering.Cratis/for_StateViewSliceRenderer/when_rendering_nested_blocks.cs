// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// A <c>nested</c> block renders as a sibling record carrying its own class-level projection attributes, which the
/// read model holds through a nullable <c>[Nested]</c> property. The compilation is the assertion that matters:
/// only the compiler can say the attributes exist and are legal where they were placed.
/// </summary>
public class when_rendering_nested_blocks : a_projection_with_nested_blocks
{
    RenderedFile _file = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_detailsSlice, _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_render_the_nested_record_as_a_sibling_type() =>
        _file.Content.ShouldContain(
            "public record InvoiceDetailsShipping([SetFrom<ShippingAddressSet>(nameof(ShippingAddressSet.Street))] string Street, " +
            "[SetFrom<ShippingAddressSet>(nameof(ShippingAddressSet.City))] string City, " +
            "[Nested] InvoiceDetailsShippingCarrier? Carrier);");

    [Fact] void should_subscribe_the_nested_record_to_the_event_its_from_block_names() =>
        _file.Content.ShouldContain("[FromEvent<ShippingAddressSet>]");
    [Fact] void should_clear_the_nested_record_with_the_event_its_clear_with_names() =>
        _file.Content.ShouldContain("[ClearWith<ShippingCleared>]");
    [Fact] void should_hold_the_nested_record_on_a_nullable_property_of_the_read_model() =>
        _file.Content.ShouldContain("[Nested] InvoiceDetailsShipping? Shipping");
    [Fact] void should_not_declare_the_nested_record_as_a_read_model() =>
        _file.Content.ShouldNotContain($"[ReadModel]{Environment.NewLine}public record InvoiceDetailsShipping");

    [Fact] void should_render_the_nested_record_inside_a_nested_record() =>
        _file.Content.ShouldContain(
            "public record InvoiceDetailsShippingCarrier([SetFrom<CarrierAssigned>(nameof(CarrierAssigned.CarrierName))] string CarrierName);");
    [Fact] void should_subscribe_the_innermost_nested_record_to_its_own_event() =>
        _file.Content.ShouldContain("[FromEvent<CarrierAssigned>]");
    [Fact] void should_disable_auto_map_on_the_nested_record_that_asks_for_it() =>
        _file.Content.ShouldContain("[NoAutoMap]");

    [Fact] void should_still_key_the_read_model_on_its_own_key() =>
        _file.Content.ShouldContain("[Key] [SetFrom<InvoiceRegistered>(nameof(InvoiceRegistered.InvoiceNumber))] string InvoiceNumber");

    // 'every' inside a 'nested' block has no established meaning on a nested type, so it is named rather than
    // rendered as an attribute nobody has confirmed the behavior of there.
    [Fact] void should_report_the_block_a_nested_type_does_not_render() =>
        _file.Diagnostics.ShouldContain(
            "Nested record 'InvoiceDetailsShipping' declares 1 every block(s) whose meaning on a nested type is not established — they are not rendered.");
    [Fact] void should_flag_that_block_in_the_file() =>
        _file.Content.ShouldContain("// TODO: 1 every block(s) not yet rendered — their meaning on a nested type is not established");
    [Fact] void should_not_report_the_nested_blocks_as_unrendered() =>
        _file.Diagnostics.ShouldNotContain(
            "Projection 'InvoiceDetails' declares 1 nested block(s) that belong to a nested or child record type nothing generates yet — they are not rendered.");
}
