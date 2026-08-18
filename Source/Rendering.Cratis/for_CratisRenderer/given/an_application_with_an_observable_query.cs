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
/// An application whose State View declares live queries: a collection the document marks <c>observable</c>, the
/// single instance behind an identifying <c>by</c> parameter marked the same way, and a plain query alongside
/// them so the rendered file has to tell the two apart.
/// </summary>
/// <remarks>
/// The point of rendering this one through the whole renderer, rather than through <c>QueryRenderer</c> alone, is
/// the compilation: <c>Observe</c> and <c>ObserveById</c> are extension methods a package puts on
/// <c>IMongoCollection&lt;T&gt;</c> and <c>ISubject</c> lives in a namespace nothing else in a rendered file
/// imports, so a string assertion would happily pass on a method the rendered application cannot build.
/// </remarks>
public class an_application_with_an_observable_query : Specification
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
                Collection("Outstanding", observable: false),
                Collection("LiveOutstanding", observable: true),
                By("LiveForInvoice", "number", observable: true),
            ],
            [projection],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoicing", [], [registerSlice, summarySlice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        _application = new ApplicationSyntax(
            [],
            [
                new ConceptSyntax("InvoiceNumber", "String", [], [], SourceLocation.Start),
                new ConceptSyntax("Money", "Decimal", [], [], SourceLocation.Start),
            ],
            [],
            [module],
            SourceLocation.Start);

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

    static QuerySyntax Collection(string name, bool observable) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static QuerySyntax By(string name, string parameter, bool observable) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", false, false, SourceLocation.Start),
            new QueryParameterSyntax(parameter, new TypeRefSyntax("InvoiceNumber", false, false, SourceLocation.Start), SourceLocation.Start),
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static ProducesSyntax Produces(string @event, params (string Property, string Source)[] mappings) =>
        new(
            @event,
            null,
            [.. mappings.Select(mapping =>
                new PropertyMappingSyntax(mapping.Property, new PathExpressionSyntax(mapping.Source, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);
}
