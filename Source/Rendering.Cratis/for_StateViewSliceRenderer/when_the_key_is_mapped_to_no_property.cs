// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_the_key_is_mapped_to_no_property : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax(
            "InvoiceRegistered",
            [
                new PropertySyntax("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
                new PropertySyntax("amount", new TypeRefSyntax("Decimal", false, false, SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            null,
            [new AddMappingSyntax("total", new PathExpressionSyntax("amount", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var projection = new ProjectionSyntax("InvoiceTotals", "InvoiceTotals", null, AutoMapMode.Enabled, null, [from], SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "Totals", [invoiceRegistered], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        _applicationSet = new ApplicationSet([new ApplicationSyntax([], [], [], [module], SourceLocation.Start)]);
    }

    void Because() => _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");

    [Fact] void should_add_a_property_carrying_the_key() => _file.Content.ShouldContain("[Key] string InvoiceNumber");
    [Fact] void should_type_the_key_property_from_the_event_it_comes_from() =>
        _file.Content.ShouldContain("public static Task<InvoiceTotals?> InvoiceTotalsById(IReadModels readModels, string invoiceNumber)");
    [Fact] void should_report_that_the_key_carries_no_mapping() =>
        _file.Diagnostics.ShouldContain("Key 'invoiceNumber' is mapped to no read model property — a 'InvoiceNumber' property was added to carry it.");
}
