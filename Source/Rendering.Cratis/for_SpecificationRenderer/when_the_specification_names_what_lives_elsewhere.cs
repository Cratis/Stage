// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

/// <summary>
/// A rendered spec sits in a child namespace of its slice, so it finds the command without an import and finds
/// nothing else. An event another slice declares, and a fanout stating one event type twice, are the two shapes
/// that turn that into output which does not compile — so both are compiled here rather than inspected.
/// </summary>
public class when_the_specification_names_what_lives_elsewhere : Specification
{
    RenderedFile _rendered = null!;
    IReadOnlyList<string> _errors = null!;

    void Because()
    {
        var invoiceId = Property("invoiceId", "InvoiceId", isIdentifier: true);
        var lineNumber = Property("lineNumber", "Int");

        // Declared by a sibling slice, which is where the rendered spec cannot see it from.
        var lineAdded = new EventSyntax("InvoiceLineAdded", [invoiceId, lineNumber], SourceLocation.Start);
        var linesSlice = new SliceSyntax(
            SliceType.StateChange, "AddLines", [lineAdded], [], [], [], [], [], [], [], [], SourceLocation.Start);

        var command = new CommandSyntax("RegisterInvoice", [invoiceId], null, [], [], null, SourceLocation.Start);
        var registerSlice = new SliceSyntax(
            SliceType.StateChange, "Register", [], [command], [], [], [], [], [], [], [], SourceLocation.Start);
        var located = new LocatedSlice(registerSlice, ["Billing", "Invoicing"]);

        var application = new ApplicationSyntax(
            [],
            [new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start)],
            [],
            [new ModuleSyntax(
                "Billing",
                [],
                [new FeatureSyntax("Invoicing", [], [registerSlice, linesSlice], SourceLocation.Start)],
                SourceLocation.Start)],
            SourceLocation.Start);
        var applicationSet = new ApplicationSet([application]);

        var specification = new Screenplay.Syntax.Specifications.SpecificationSyntax(
            "AddingTwoLines",
            [],
            new Screenplay.Syntax.Specifications.SpecificationCommandSyntax(
                "RegisterInvoice",
                [Value("invoiceId", "9c858901-8a57-4791-81fe-4c455b099bc9")],
                SourceLocation.Start),
            [
                Then("InvoiceLineAdded", ("lineNumber", 1)),
                Then("InvoiceLineAdded", ("lineNumber", 2)),
            ],
            [],
            SourceLocation.Start);

        _rendered = SpecificationRenderer.Render(specification, command, located, applicationSet, "Acme");

        var slices = new[] { located, new LocatedSlice(linesSlice, ["Billing", "Invoicing"]) }
            .Select(slice => new StateChangeSliceRenderer().Render(slice, applicationSet, "Acme"));
        var concepts = applicationSet.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, applicationSet, "Acme"));
        _errors = RenderedOutput.Errors([_rendered, .. slices, .. concepts]);
    }

    [Fact] void should_render_output_that_compiles() => _errors.ShouldBeEmpty();
    [Fact] void should_import_the_slice_that_declares_the_event() =>
        _rendered.Content.ShouldContain("using Acme.Billing.Invoicing.AddLines;");
    [Fact] void should_name_the_two_facts_apart() =>
        _rendered.Content.ShouldContain("should_have_appended_invoice_line_added_1()");
    [Fact] void should_name_the_second_fact_for_its_occurrence() =>
        _rendered.Content.ShouldContain("should_have_appended_invoice_line_added_2()");

    static PropertySyntax Property(string name, string type, bool isIdentifier = false) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: isIdentifier);

    static PropertyMappingSyntax Value(string property, object value) =>
        new(property, new LiteralExpressionSyntax(value, SourceLocation.Start), SourceLocation.Start);

    static Screenplay.Syntax.Specifications.SpecificationEventSyntax Then(string type, params (string Property, object Value)[] values) =>
        new(type, [.. values.Select(value => Value(value.Property, value.Value))], SourceLocation.Start);
}
