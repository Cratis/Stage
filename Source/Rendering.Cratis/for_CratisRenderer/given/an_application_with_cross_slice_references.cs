// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Renderers;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

/// <summary>
/// An application whose slices reference each other the way real applications do: a State View in one feature
/// projects an event declared by a State Change slice in another, and an Automation slice in a third reacts to
/// the same event. Nothing here is exotic — this is the shape almost every Cratis application has, and the shape
/// that produced most of the non-compiling output.
/// </summary>
public class an_application_with_cross_slice_references : Specification
{
    protected InMemoryCodeOutput _codeOutput = null!;
    protected CratisRenderer _renderer = null!;
    protected ApplicationSyntax _application = null!;
    protected ModuleSyntax _module = null!;
    protected FeatureSyntax _reportingFeature = null!;
    protected SliceSyntax _summarySlice = null!;
    protected DirectoryInfo _targetDirectory = null!;
    protected StringWriter _output = null!;
    protected StringWriter _error = null!;

    void Establish()
    {
        var invoiceNumber = Property("invoiceNumber", "InvoiceNumber", isIdentifier: true);
        var amount = Property("amount", "Money");

        var command = new CommandSyntax(
            "RegisterInvoice",
            [invoiceNumber, amount],
            null,
            [],
            [Produces("InvoiceRegistered", ("invoiceNumber", "invoiceNumber"), ("amount", "amount"))],
            null,
            SourceLocation.Start);

        var invoiceRegistered = new EventSyntax("InvoiceRegistered", [invoiceNumber, amount], SourceLocation.Start);

        var registerSlice = new SliceSyntax(
            SliceType.StateChange, "Register", [invoiceRegistered], [command], [], [], [], [], [], [], [], SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            null,
            [
                new SetMappingSyntax("number", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
                new SetMappingSyntax("amount", new PathExpressionSyntax("amount", SourceLocation.Start), SourceLocation.Start),
                new CountMappingSyntax("registrations", SourceLocation.Start),
            ],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "InvoiceSummary", "InvoiceSummary", null, AutoMapMode.Enabled, null, [from], SourceLocation.Start);

        _summarySlice = new SliceSyntax(
            SliceType.StateView, "Summary", [], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var reaction = new ReactionSyntax(
            "InvoiceNotifications",
            [new ReactionTriggerSyntax(new NamedTriggerSourceSyntax("InvoiceRegistered", SourceLocation.Start), [], null, null, SourceLocation.Start)],
            SourceLocation.Start);

        var notifySlice = new SliceSyntax(
            SliceType.Automation, "Notify", [], [], [], [], [], [reaction], [], [], [], SourceLocation.Start);

        var registrationFeature = new FeatureSyntax("Registration", [], [registerSlice], SourceLocation.Start);
        _reportingFeature = new FeatureSyntax("Reporting", [], [_summarySlice], SourceLocation.Start);
        var notificationsFeature = new FeatureSyntax("Notifications", [], [notifySlice], SourceLocation.Start);

        _module = new ModuleSyntax("Billing", [], [registrationFeature, _reportingFeature, notificationsFeature], SourceLocation.Start);

        _application = new ApplicationSyntax(
            [],
            [
                new ConceptSyntax("InvoiceNumber", "String", [], [], SourceLocation.Start),
                new ConceptSyntax("Money", "Decimal", [], [], SourceLocation.Start),
            ],
            [],
            [_module],
            SourceLocation.Start);

        _codeOutput = new InMemoryCodeOutput();
        var reactionRenderer = new ReactionSliceRenderer();
        _renderer = new CratisRenderer(
            new a_stub_scaffolder(),
            new Dictionary<SliceType, ISliceRenderer>
            {
                [SliceType.StateChange] = new StateChangeSliceRenderer(),
                [SliceType.StateView] = new StateViewSliceRenderer(),
                [SliceType.Automation] = reactionRenderer,
                [SliceType.Translate] = reactionRenderer,
            },
            _codeOutput);

        _targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "AcmeBilling"));
        _output = new StringWriter();
        _error = new StringWriter();
    }

    static PropertySyntax Property(string name, string type, bool isIdentifier = false) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: isIdentifier);

    static ProducesSyntax Produces(string @event, params (string Property, string Source)[] mappings) =>
        new(
            @event,
            null,
            [.. mappings.Select(mapping =>
                new PropertyMappingSyntax(mapping.Property, new PathExpressionSyntax(mapping.Source, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);
}
