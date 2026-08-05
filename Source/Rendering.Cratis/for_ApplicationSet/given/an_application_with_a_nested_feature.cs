// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_ApplicationSet.given;

public class an_application_with_a_nested_feature : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected SliceSyntax _registerSlice = null!;

    void Establish()
    {
        var invoiceIdConcept = new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start);

        var identifierProperty = new PropertySyntax(
            "invoiceId",
            new TypeRefSyntax("InvoiceId", false, false, SourceLocation.Start),
            SourceLocation.Start,
            IsIdentifier: true);

        var command = new CommandSyntax("RegisterInvoice", [identifierProperty], null, [], [], null, SourceLocation.Start);

        _registerSlice = new SliceSyntax(
            SliceType.StateChange,
            "Register",
            [],
            [command],
            [],
            null,
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var detailsFeature = new FeatureSyntax("Details", [], [_registerSlice], SourceLocation.Start);
        var invoicesFeature = new FeatureSyntax("Invoices", [detailsFeature], [], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [invoicesFeature], SourceLocation.Start);

        var application = new ApplicationSyntax([], [invoiceIdConcept], [], [module], SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
    }
}
