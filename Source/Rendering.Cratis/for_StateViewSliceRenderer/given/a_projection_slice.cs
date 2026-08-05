// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

public class a_projection_slice : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _summarySlice = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax(
            "InvoiceRegistered",
            [
                new PropertySyntax("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
                new PropertySyntax("customerId", new TypeRefSyntax("Uuid", false, false, SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var invoiceSent = new EventSyntax(
            "InvoiceSent",
            [new PropertySyntax("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var registeredFrom = new FromSyntax(
            [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
            null,
            null,
            [
                new SetMappingSyntax("invoiceNumber", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
                new IncrementMappingSyntax("totalCount", SourceLocation.Start),
            ],
            SourceLocation.Start);

        var sentFrom = new FromSyntax(
            [new EventSpecSyntax("InvoiceSent", null, SourceLocation.Start)],
            null,
            null,
            [new DecrementMappingSyntax("draftCount", SourceLocation.Start)],
            SourceLocation.Start);

        var join = new JoinSyntax("customer", "customerId", [], SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "InvoiceSummary",
            "InvoiceSummary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            [registeredFrom, sentFrom, join],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "InvoiceSummary",
            [invoiceRegistered, invoiceSent],
            [],
            [],
            projection,
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        var application = new ApplicationSyntax([], [], [], [module], SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
        _summarySlice = _applicationSet.Slices.Single();
    }
}
