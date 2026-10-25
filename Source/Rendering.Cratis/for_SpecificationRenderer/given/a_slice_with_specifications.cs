// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;

/// <summary>
/// A State Change slice whose command is exercised by specifications of every shape the language allows.
/// </summary>
public class a_slice_with_specifications : Specification
{
    protected CommandSyntax _command = null!;
    protected SliceSyntax _sliceSyntax = null!;
    protected LocatedSlice _slice = null!;
    protected ApplicationSet _applicationSet = null!;

    void Establish()
    {
        var invoiceId = Property("invoiceId", "InvoiceId", isIdentifier: true);
        var invoiceNumber = Property("invoiceNumber", "String");
        var dueDate = Property("dueDate", "Date");
        var registered = new EventSyntax("InvoiceRegistered", [invoiceId, invoiceNumber, dueDate], SourceLocation.Start);

        _command = new CommandSyntax("RegisterInvoice", [invoiceId, invoiceNumber, dueDate], null, [], [], null, SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateChange, "Register", [registered], [_command], [], [], [], [], [], [], [], SourceLocation.Start);
        _sliceSyntax = slice;
        _slice = new LocatedSlice(slice, ["Billing", "Invoicing"]);

        var application = new ApplicationSyntax(
            [],
            [new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start)],
            [],
            [new ModuleSyntax("Billing", [], [new FeatureSyntax("Invoicing", [], [slice], SourceLocation.Start)], SourceLocation.Start)],
            SourceLocation.Start);
        _applicationSet = new ApplicationSet([application]);
    }

    protected static SpecificationSyntax Specification(
        string name,
        IEnumerable<SpecificationEventSyntax>? given = null,
        SpecificationCommandSyntax? when = null,
        IEnumerable<SpecificationEventSyntax>? then = null,
        IEnumerable<SpecificationErrorSyntax>? errors = null,
        IEnumerable<SpecificationReadModelSyntax>? thenReadModels = null) =>
        new(name, given ?? [], when, then ?? [], errors ?? [], SourceLocation.Start, ThenReadModels: thenReadModels);

    protected static SpecificationCommandSyntax When(string command, params (string Property, object Value)[] values) =>
        new(command, [.. values.Select(Mapping)], SourceLocation.Start);

    protected static SpecificationEventSyntax Event(string type, params (string Property, object Value)[] values) =>
        new(type, [.. values.Select(Mapping)], SourceLocation.Start);

    static PropertyMappingSyntax Mapping((string Property, object Value) value) =>
        new(value.Property, new LiteralExpressionSyntax(value.Value, SourceLocation.Start), SourceLocation.Start);

    static PropertySyntax Property(string name, string type, bool isIdentifier = false) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: isIdentifier);
}
