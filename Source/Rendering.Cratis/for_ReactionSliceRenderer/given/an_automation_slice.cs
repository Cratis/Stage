// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer.given;

public class an_automation_slice : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _detectorSlice = null!;

    void Establish()
    {
        var invoiceRegistered = new EventSyntax(
            "InvoiceRegistered",
            [new PropertySyntax("dueDate", new TypeRefSyntax("DateTime", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var codeTrigger = new ReactionTriggerSyntax(
            new NamedTriggerSourceSyntax("InvoiceRegistered", SourceLocation.Start),
            [],
            null,
            new CodeBlockSyntax(
                "csharp",
                "if (DueDate < DateTimeOffset.UtcNow)\n    return [new MarkInvoiceOverdue(DueDate)];\nreturn null;",
                SourceLocation.Start),
            SourceLocation.Start);

        var fileTrigger = new ReactionTriggerSyntax(new NamedTriggerSourceSyntax("InvoiceSent", SourceLocation.Start), [], new FileReferenceSyntax("Reactors/NotifyCustomerReactor.cs", SourceLocation.Start), null, SourceLocation.Start);

        var bareTrigger = new ReactionTriggerSyntax(new NamedTriggerSourceSyntax("InvoicePaid", SourceLocation.Start), [], null, null, SourceLocation.Start);

        var reaction = new ReactionSyntax(
            "OverdueInvoiceDetector", [codeTrigger, fileTrigger, bareTrigger], SourceLocation.Start, "Detects overdue invoices");

        var slice = new SliceSyntax(
            SliceType.Automation,
            "DetectOverdueInvoices",
            [invoiceRegistered],
            [],
            [],
            [],
            [],
            [reaction],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        var application = new ApplicationSyntax([], [], [], [module], SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
        _detectorSlice = _applicationSet.Slices.Single();
    }
}
