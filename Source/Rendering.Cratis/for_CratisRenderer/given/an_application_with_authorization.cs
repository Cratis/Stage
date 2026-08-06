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
/// An application that states who may do what: policies at the top, an <c>authorize</c> on commands and on the
/// queries of a State View, alongside the constructs that surround them in a real document — a uniqueness
/// constraint, a screen, a persona and an authentication provider. Every one of them was silently dropped.
/// </summary>
public class an_application_with_authorization : Specification
{
    protected InMemoryCodeOutput _codeOutput = null!;
    protected CratisRenderer _renderer = null!;
    protected ApplicationSyntax _application = null!;
    protected DirectoryInfo _targetDirectory = null!;
    protected StringWriter _output = null!;
    protected StringWriter _error = null!;

    void Establish()
    {
        var invoiceNumber = Property("invoiceNumber", "InvoiceNumber", isIdentifier: true);
        var amount = Property("amount", "Money");
        var invoiceRegistered = new EventSyntax("InvoiceRegistered", [invoiceNumber, amount], SourceLocation.Start);

        var register = new CommandSyntax(
            "RegisterInvoice",
            [invoiceNumber, amount],
            Authorize("Administrator", "Auditor"),
            [],
            [Produces("InvoiceRegistered", ("invoiceNumber", "invoiceNumber"), ("amount", "amount"))],
            null,
            SourceLocation.Start);

        var registerSlice = new SliceSyntax(
            SliceType.StateChange,
            "Register",
            [invoiceRegistered],
            [register],
            [],
            null,
            [],
            [],
            [],
            [new UniquePropertyConstraintSyntax("UniqueInvoiceNumber", "invoiceNumber", "InvoiceRegistered", SourceLocation.Start)],
            [],
            SourceLocation.Start);

        var archive = new CommandSyntax("ArchiveInvoice", [invoiceNumber], null, [], [], null, SourceLocation.Start);
        var archiveSlice = new SliceSyntax(
            SliceType.StateChange, "Archive", [], [archive], [], null, [], [], [], [], [], SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
            new ExpressionKeySyntax(new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
            null,
            [
                new SetMappingSyntax("number", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
                new SetMappingSyntax("amount", new PathExpressionSyntax("amount", SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "InvoiceSummary", "InvoiceSummary", null, AutoMapMode.Enabled, null, [from], SourceLocation.Start);

        var summarySlice = new SliceSyntax(
            SliceType.StateView,
            "Summary",
            [],
            [],
            [
                Query("All", Authorize("Administrator")),
                Query("Mine", Authorize("Auditor")),
            ],
            projection,
            [],
            [],
            [new ScreenSyntax("Invoices", new FileReferenceSyntax("Invoices.tsx", SourceLocation.Start), [], SourceLocation.Start)],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoicing", [], [registerSlice, archiveSlice, summarySlice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        _application = new ApplicationSyntax(
            [],
            [
                new ConceptSyntax("InvoiceNumber", "String", [], [], SourceLocation.Start),
                new ConceptSyntax("Money", "Decimal", [], [], SourceLocation.Start),
            ],
            [
                new PolicySyntax("Administrator", new RoleConditionSyntax("Administrator", SourceLocation.Start), null, SourceLocation.Start),
                new PolicySyntax("Auditor", new RoleConditionSyntax("Auditor", SourceLocation.Start), null, SourceLocation.Start),
            ],
            [module],
            SourceLocation.Start,
            Personas: [new PersonaSyntax("Accountant", null, ["Auditor"], SourceLocation.Start)],
            Authentication: new AuthenticationSyntax([new AuthenticationProviderSyntax("entra", [], SourceLocation.Start)], SourceLocation.Start));

        _codeOutput = new InMemoryCodeOutput();
        _renderer = new CratisRenderer(
            new a_stub_scaffolder(),
            new Dictionary<SliceType, ISliceRenderer>
            {
                [SliceType.StateChange] = new StateChangeSliceRenderer(),
                [SliceType.StateView] = new StateViewSliceRenderer(),
            },
            _codeOutput);

        _targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "AcmeBilling"));
        _output = new StringWriter();
        _error = new StringWriter();
    }

    static PropertySyntax Property(string name, string type, bool isIdentifier = false) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: isIdentifier);

    static AuthorizeSyntax Authorize(params string[] policies) =>
        new([.. policies.Select((policy, index) => new PolicyReferenceSyntax(policy, index > 0, SourceLocation.Start))], SourceLocation.Start);

    static QuerySyntax Query(string name, AuthorizeSyntax authorize) =>
        new(name, new TypeRefSyntax("InvoiceSummary", true, false, SourceLocation.Start), null, [], authorize, SourceLocation.Start);

    static ProducesSyntax Produces(string @event, params (string Property, string Source)[] mappings) =>
        new(
            @event,
            null,
            [.. mappings.Select(mapping =>
                new PropertyMappingSyntax(mapping.Property, new PathExpressionSyntax(mapping.Source, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);
}
