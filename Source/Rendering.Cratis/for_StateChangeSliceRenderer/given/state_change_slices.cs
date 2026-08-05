// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;

public class state_change_slices : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _registerInvoice = null!;
    protected LocatedSlice _processBatch = null!;
    protected LocatedSlice _cancelInvoice = null!;

    void Establish()
    {
        var invoiceIdConcept = new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start);

        // RegisterInvoice: identifier via a declared concept, one validation rule, one unconditional produces.
        var registerIdentifier = new PropertySyntax(
            "invoiceId", new TypeRefSyntax("InvoiceId", false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: true);
        var registerName = new PropertySyntax("name", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start);

        var nameRule = new ValidationRuleSyntax("name", ValidationRuleKind.NotEmpty, null, "Name is required", SourceLocation.Start);
        var registerValidations = new ValidateSyntax[] { new DeclarativeValidateSyntax([nameRule], SourceLocation.Start) };

        var registerProduces = new ProducesSyntax(
            "InvoiceRegistered",
            null,
            [new PropertyMappingSyntax("name", new PathExpressionSyntax("name", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var registerCommand = new CommandSyntax(
            "RegisterInvoice", [registerIdentifier, registerName], null, registerValidations, [registerProduces], null, SourceLocation.Start);

        var invoiceRegisteredEvent = new EventSyntax(
            "InvoiceRegistered",
            [new PropertySyntax("name", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var registerSlice = new SliceSyntax(
            SliceType.StateChange, "RegisterInvoice", [invoiceRegisteredEvent], [registerCommand], [], null, [], [], [], [], [], SourceLocation.Start);

        // ProcessBatch: imperative handler.
        var handlerCode = new CodeBlockSyntax(
            "csharp",
            "var events = new List<object>();\nevents.Add(new BatchProcessed(context.Identity.Id));\nreturn events;",
            SourceLocation.Start);
        var batchIdProperty = new PropertySyntax("batchId", new TypeRefSyntax("Uuid", false, false, SourceLocation.Start), SourceLocation.Start);
        var processCommand = new CommandSyntax(
            "ProcessBatch", [batchIdProperty], null, [], [], new HandlerSyntax(null, handlerCode, SourceLocation.Start), SourceLocation.Start);
        var processSlice = new SliceSyntax(
            SliceType.StateChange, "ProcessBatch", [], [processCommand], [], null, [], [], [], [], [], SourceLocation.Start);

        // CancelInvoice: identifier with no declared concept (raw Uuid) + multiple conditional produces.
        var cancelIdentifier = new PropertySyntax(
            "invoiceId", new TypeRefSyntax("Uuid", false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: true);
        var reasonProperty = new PropertySyntax("reason", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start);

        var cancelledCondition = new ComparisonConditionSyntax(
            "reason", ComparisonOperator.NotEqual, new LiteralExpressionSyntax(string.Empty, SourceLocation.Start), SourceLocation.Start);

        var cancelledProduces = new ProducesSyntax(
            "InvoiceCancelled",
            cancelledCondition,
            [new PropertyMappingSyntax("reason", new PathExpressionSyntax("reason", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var refundProduces = new ProducesSyntax("InvoiceRefundRequested", null, [], SourceLocation.Start);

        var cancelCommand = new CommandSyntax(
            "CancelInvoice",
            [cancelIdentifier, reasonProperty],
            null,
            [],
            [cancelledProduces, refundProduces],
            null,
            SourceLocation.Start);

        var cancelSlice = new SliceSyntax(
            SliceType.StateChange, "CancelInvoice", [], [cancelCommand], [], null, [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [registerSlice, processSlice, cancelSlice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        var application = new ApplicationSyntax([], [invoiceIdConcept], [], [module], SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
        _registerInvoice = _applicationSet.Slices.Single(slice => slice.Slice.Name == "RegisterInvoice");
        _processBatch = _applicationSet.Slices.Single(slice => slice.Slice.Name == "ProcessBatch");
        _cancelInvoice = _applicationSet.Slices.Single(slice => slice.Slice.Name == "CancelInvoice");
    }
}
