// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Renderers;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

public class a_multi_slice_application : Specification
{
    protected InMemoryCodeOutput _codeOutput = null!;
    protected a_stub_scaffolder _scaffolder = null!;
    protected CratisRenderer _renderer = null!;
    protected ApplicationSyntax _application = null!;
    protected DirectoryInfo _targetDirectory = null!;
    protected StringWriter _output = null!;
    protected StringWriter _error = null!;

    void Establish()
    {
        var invoiceIdConcept = new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start);

        var identifierProperty = new PropertySyntax(
            "invoiceId", new TypeRefSyntax("InvoiceId", false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: true);
        var nameProperty = new PropertySyntax("name", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start);

        var produces = new ProducesSyntax(
            "InvoiceRegistered",
            null,
            [new PropertyMappingSyntax("name", new PathExpressionSyntax("name", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var command = new CommandSyntax(
            "RegisterInvoice", [identifierProperty, nameProperty], null, [], [produces], null, SourceLocation.Start);

        var invoiceRegisteredEvent = new EventSyntax("InvoiceRegistered", [nameProperty], SourceLocation.Start);

        var registerSlice = new SliceSyntax(
            SliceType.StateChange, "RegisterInvoice", [invoiceRegisteredEvent], [command], [], [], [], [], [], [], [], SourceLocation.Start);

        var summaryFrom = new FromSyntax(
            [new EventSpecSyntax("InvoiceRegistered", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("name", new PathExpressionSyntax("name", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var summaryProjection = new ProjectionSyntax(
            "InvoiceSummary", "InvoiceSummary", null, AutoMapMode.Enabled, null, [summaryFrom], SourceLocation.Start);

        var summarySlice = new SliceSyntax(
            SliceType.StateView, "InvoiceSummary", [], [], [], [summaryProjection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [registerSlice, summarySlice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        _application = new ApplicationSyntax([], [invoiceIdConcept], [], [module], SourceLocation.Start);

        _codeOutput = new InMemoryCodeOutput();
        _scaffolder = new a_stub_scaffolder();
        _renderer = new CratisRenderer(
            _scaffolder,
            new Dictionary<SliceType, ISliceRenderer>
            {
                [SliceType.StateChange] = new StateChangeSliceRenderer(),
                [SliceType.StateView] = new StateViewSliceRenderer(),
            },
            _codeOutput);

        _targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cratis-renderer-spec"));
        _output = new StringWriter();
        _error = new StringWriter();
    }
}
