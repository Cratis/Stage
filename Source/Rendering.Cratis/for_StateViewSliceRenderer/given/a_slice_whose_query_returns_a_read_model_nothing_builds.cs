// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

/// <summary>
/// A slice building one read model and declaring one query whose return type names a read model no projection in
/// the slice builds — a name that resolves to nothing the rendered application holds.
/// </summary>
public class a_slice_whose_query_returns_a_read_model_nothing_builds : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _dashboardSlice = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax(
            "InvoiceRegistered",
            [new PropertySyntax("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var summaryProjection = new ProjectionSyntax(
            "InvoiceSummary",
            "InvoiceSummary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            [
                new FromSyntax(
                    [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
                    null,
                    null,
                    [new SetMappingSyntax("invoiceNumber", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start)],
                    SourceLocation.Start)
            ],
            SourceLocation.Start);

        var overdueQuery = new QuerySyntax(
            "GetOverdueInvoices",
            new TypeRefSyntax("OverdueInvoices", true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "InvoiceDashboard",
            [invoiceRegistered],
            [],
            [overdueQuery],
            [summaryProjection],
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
        _dashboardSlice = _applicationSet.Slices.Single();
    }
}
