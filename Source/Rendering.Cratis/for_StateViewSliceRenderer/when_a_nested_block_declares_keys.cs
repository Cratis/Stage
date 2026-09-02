// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// The key a nested <c>from</c> declares is what routes its events to a document. Chronicle applies a class-level
/// <c>[FromEvent]</c>'s key and parent key to the nested definition's own <c>From</c>, and that is the key
/// resolver the projection is built with — so dropping the key would not lose a detail, it would write the nested
/// object onto whatever the event source id points at. A key written on one event wins over the block's, which is
/// the order the kernel resolves the same syntax in.
/// </summary>
public class when_a_nested_block_declares_keys : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Establish()
    {
        var registered = Event("OrderRegistered", "orderNumber");
        var addressSet = Event("ShippingAddressSet", "orderNumber", "street");
        var carrierAssigned = Event("CarrierAssigned", "shipmentRef", "carrierName");

        var root = new FromSyntax(
            [new EventSpecSyntax("OrderRegistered", null, SourceLocation.Start)],
            null,
            null,
            [Set("orderNumber", "orderNumber")],
            SourceLocation.Start);

        // A block-level key, and an event carrying its own key that must win over it.
        var nestedFrom = new FromSyntax(
            [
                new EventSpecSyntax("ShippingAddressSet", null, SourceLocation.Start),
                new EventSpecSyntax("CarrierAssigned", new PathExpressionSyntax("shipmentRef", SourceLocation.Start), SourceLocation.Start),
            ],
            new ExpressionKeySyntax(new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start),
            new PathExpressionSyntax("orderNumber", SourceLocation.Start),
            [Set("street", "street")],
            SourceLocation.Start);

        var nested = new NestedSyntax("shipping", AutoMapMode.Inherit, [nestedFrom], SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "OrderShipping",
            "OrderShipping",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start),
            [root, nested],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "OrderShipping", [registered, addressSet, carrierAssigned], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Orders", [], [slice], SourceLocation.Start);
        _applicationSet = new ApplicationSet(
            [new ApplicationSyntax([], [], [], [new ModuleSyntax("Sales", [], [feature], SourceLocation.Start)], SourceLocation.Start)]);
    }

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    [Fact] void should_render_output_that_compiles() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_carry_the_block_key_onto_the_event_that_declares_none() =>
        _file.Content.ShouldContain(
            "[FromEvent<ShippingAddressSet>(key: nameof(ShippingAddressSet.OrderNumber), parentKey: nameof(ShippingAddressSet.OrderNumber))]");

    [Fact] void should_let_a_key_written_on_one_event_win_over_the_block_key() =>
        _file.Content.ShouldContain("[FromEvent<CarrierAssigned>(key: nameof(CarrierAssigned.ShipmentRef)");

    [Fact] void should_not_route_a_keyed_nested_event_on_the_event_source_id() =>
        _file.Content.ShouldNotContain("[FromEvent<ShippingAddressSet>]");

    static EventSyntax Event(string name, params string[] properties) =>
        new(
            name,
            [.. properties.Select(property =>
                new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);

    static SetMappingSyntax Set(string property, string source) =>
        new(property, new PathExpressionSyntax(source, SourceLocation.Start), SourceLocation.Start);
}
