// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

/// <summary>
/// An invoice details projection with a <c>children</c> block of line items identified by their line number,
/// mapping from two events, removed by a third, holding a further <c>children</c> block of allocations with
/// automapping disabled, and declaring an <c>every</c> block whose meaning on a child type is not established.
/// </summary>
public class a_projection_with_children_blocks : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _detailsSlice = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax("InvoiceRegistered", [Property("invoiceNumber", "String")], SourceLocation.Start);
        var lineItemAdded = new EventSyntax(
            "InvoiceLineItemAdded",
            [
                Property("lineNumber", "Int"),
                Property("description", "String"),
                Property("unitPrice", "Decimal"),
                Property("invoiceNumber", "String"),
            ],
            SourceLocation.Start);
        var lineItemPriced = new EventSyntax(
            "InvoiceLineItemPriced",
            [
                Property("lineNumber", "Int"),
                Property("unitPrice", "Decimal"),
            ],
            SourceLocation.Start);
        var lineItemRemoved = new EventSyntax(
            "InvoiceLineItemRemoved",
            [
                Property("lineNumber", "Int"),
                Property("invoiceNumber", "String"),
            ],
            SourceLocation.Start);
        var allocationAdded = new EventSyntax(
            "InvoiceLineAllocationAdded",
            [
                Property("allocationNumber", "Int"),
                Property("account", "String"),
            ],
            SourceLocation.Start);

        var registered = From("InvoiceRegistered", null, ("invoiceNumber", "invoiceNumber"));

        // A children block inside a children block. Its 'identified by' names a property nothing maps, so the
        // renderer has to add one to carry it.
        var allocations = new ChildrenSyntax(
            "allocations",
            new PathExpressionSyntax("allocationNumber", SourceLocation.Start),
            AutoMapMode.Disabled,
            [From("InvoiceLineAllocationAdded", "allocationNumber", ("account", "account"))],
            SourceLocation.Start);

        var lineItems = new ChildrenSyntax(
            "lineItems",
            new PathExpressionSyntax("lineNumber", SourceLocation.Start),
            AutoMapMode.Inherit,
            [
                From("InvoiceLineItemAdded", "lineNumber", ("lineNumber", "lineNumber"), ("description", "description")),
                From("InvoiceLineItemPriced", "lineNumber", ("unitPrice", "unitPrice")),
                new RemoveWithSyntax(
                    "InvoiceLineItemRemoved",
                    new PathExpressionSyntax("lineNumber", SourceLocation.Start),
                    new PathExpressionSyntax("invoiceNumber", SourceLocation.Start),
                    SourceLocation.Start),
                new EverySyntax([], true, AutoMapMode.Inherit, SourceLocation.Start),
                allocations,
            ],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "InvoiceDetails",
            "InvoiceDetails",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            [registered, lineItems],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "InvoiceDetails",
            [invoiceRegistered, lineItemAdded, lineItemPriced, lineItemRemoved, allocationAdded],
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

    static FromSyntax From(string @event, string? key, params (string Property, string Source)[] mappings) =>
        new(
            [new EventSpecSyntax(@event, null, SourceLocation.Start)],
            key is null ? null : new ExpressionKeySyntax(new PathExpressionSyntax(key, SourceLocation.Start), SourceLocation.Start),
            null,
            [.. mappings.Select(mapping =>
                new SetMappingSyntax(mapping.Property, new PathExpressionSyntax(mapping.Source, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);
}
