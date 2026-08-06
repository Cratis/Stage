// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_UnrenderedConstructs.given;

/// <summary>
/// A slice declaring one of every construct family no renderer emits anything for.
/// </summary>
public class a_slice_declaring_every_family : Specification
{
    protected SliceSyntax _slice = null!;

    void Establish()
    {
        var query = new QuerySyntax(
            "All",
            new TypeRefSyntax("InvoiceSummary", true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            Performer: new PerformerSyntax(null, new CodeBlockSyntax("sql", "select 1", SourceLocation.Start), SourceLocation.Start));

        var capture = new CaptureSyntax(
            "Ledger", new CaptureSourceSyntax("csv", [], SourceLocation.Start), null, [], [], [], [], SourceLocation.Start);

        var screen = new ScreenSyntax("Invoices", new FileReferenceSyntax("Invoices.tsx", SourceLocation.Start), [], SourceLocation.Start);

        var constraint = new UniquePropertyConstraintSyntax(
            "UniqueInvoiceNumber", "invoiceNumber", "InvoiceRegistered", SourceLocation.Start);

        var specification = new SpecificationSyntax(
            "WhenRegistering",
            [],
            new SpecificationCommandSyntax("RegisterInvoice", [], SourceLocation.Start),
            [],
            [],
            SourceLocation.Start);

        _slice = new SliceSyntax(
            SliceType.StateView,
            "Summary",
            [],
            [],
            [query],
            null,
            [capture],
            [],
            [screen],
            [constraint],
            [specification],
            SourceLocation.Start);
    }
}
