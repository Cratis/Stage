// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

/// <summary>
/// A dashboard slice shaped like the one in the invoicing sample: two projections, and a query for each of the
/// read models they build. The rendered read model's own query is guarded by a role; the other read model's
/// query is not guarded at all.
/// </summary>
public class a_slice_with_two_read_models : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _dashboardSlice = null!;

    void Establish()
    {
        var invoiceRegistered = Event("InvoiceRegistered");
        var invoiceMarkedOverdue = Event("InvoiceMarkedOverdue");

        var summaryProjection = Projection("InvoiceSummary", "InvoiceRegistered");
        var overdueProjection = Projection("OverdueInvoices", "InvoiceMarkedOverdue");

        var summaryQuery = Query("GetInvoiceSummary", "InvoiceSummary", isCollection: false, policy: "Accountant");
        var overdueQuery = Query("GetOverdueInvoices", "OverdueInvoices", isCollection: true, policy: null);

        var slice = new SliceSyntax(
            SliceType.StateView,
            "InvoiceDashboard",
            [invoiceRegistered, invoiceMarkedOverdue],
            [],
            [summaryQuery, overdueQuery],
            [summaryProjection, overdueProjection],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        var application = new ApplicationSyntax(
            [],
            [],
            [new PolicySyntax("Accountant", new RoleConditionSyntax("Accountant", SourceLocation.Start), null, SourceLocation.Start)],
            [module],
            SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
        _dashboardSlice = _applicationSet.Slices.Single();
    }

    static EventSyntax Event(string name) =>
        new(
            name,
            [new PropertySyntax("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

    static ProjectionSyntax Projection(string readModel, string @event) =>
        new(
            readModel,
            readModel,
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            [
                new FromSyntax(
                    [new EventSpecSyntax(@event, null, SourceLocation.Start)],
                    null,
                    null,
                    [new SetMappingSyntax("invoiceNumber", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start)],
                    SourceLocation.Start)
            ],
            SourceLocation.Start);

    static QuerySyntax Query(string name, string readModel, bool isCollection, string? policy) =>
        new(
            name,
            new TypeRefSyntax(readModel, isCollection, false, SourceLocation.Start),
            null,
            [],
            policy is null ? null : new AuthorizeSyntax(new PolicyReferenceSyntax(policy, SourceLocation.Start), SourceLocation.Start),
            SourceLocation.Start);
}
