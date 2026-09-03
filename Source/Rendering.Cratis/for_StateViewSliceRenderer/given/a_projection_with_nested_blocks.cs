// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

/// <summary>
/// An invoice details projection with a <c>nested</c> shipping object that maps from its own event, is cleared by
/// another, holds a further <c>nested</c> carrier object with automapping disabled, and declares an <c>every</c>
/// block whose meaning on a nested type is not established.
/// </summary>
public class a_projection_with_nested_blocks : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _detailsSlice = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax("InvoiceRegistered", [Property("invoiceNumber", "String")], SourceLocation.Start);
        var shippingAddressSet = new EventSyntax(
            "ShippingAddressSet",
            [
                Property("street", "String"),
                Property("city", "String"),
            ],
            SourceLocation.Start);
        var shippingCleared = new EventSyntax("ShippingCleared", [], SourceLocation.Start);
        var carrierAssigned = new EventSyntax("CarrierAssigned", [Property("carrierName", "String")], SourceLocation.Start);

        var registered = From("InvoiceRegistered", ("invoiceNumber", "invoiceNumber"));

        var carrier = new NestedSyntax(
            "carrier",
            AutoMapMode.Disabled,
            [From("CarrierAssigned", ("carrierName", "carrierName"))],
            SourceLocation.Start);

        var shipping = new NestedSyntax(
            "shipping",
            AutoMapMode.Inherit,
            [
                From("ShippingAddressSet", ("street", "street"), ("city", "city")),
                new ClearWithSyntax("ShippingCleared", SourceLocation.Start),
                new EverySyntax([], true, AutoMapMode.Inherit, SourceLocation.Start),
                carrier,
            ],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "InvoiceDetails",
            "InvoiceDetails",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            [registered, shipping],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "InvoiceDetails",
            [invoiceRegistered, shippingAddressSet, shippingCleared, carrierAssigned],
            [],
            [],
            [projection],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        _applicationSet = new ApplicationSet([new ApplicationSyntax([], [], [], [module], SourceLocation.Start)]);
        _detailsSlice = _applicationSet.Slices.Single();
    }

    static PropertySyntax Property(string name, string type) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start);

    static FromSyntax From(string @event, params (string Property, string Source)[] mappings) =>
        new(
            [new EventSpecSyntax(@event, null, SourceLocation.Start)],
            null,
            null,
            [.. mappings.Select(mapping =>
                new SetMappingSyntax(mapping.Property, new PathExpressionSyntax(mapping.Source, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);
}
