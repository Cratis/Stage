// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_importing_common_for_command_specification_values : Specification
{
    SemanticApplicationContext _context = null!;
    SemanticSpecification _specification = null!;
    SemanticProperty _note = null!;
    SemanticProperty _notes = null!;
    SemanticEventContract _unusedEvent = null!;
    string _withoutConstructors = null!;
    string _withCommandConstructor = null!;
    string _withCollectionConstructor = null!;
    string _withExpectedConstructor = null!;
    string _rejected = null!;

    void Establish()
    {
        var source = "concept Note : String\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("command IssueInvoice\n", "command IssueInvoice\n        note Note?\n        notes Note[]?\n", StringComparison.Ordinal)
            .Replace("when IssueInvoice\n", "when IssueInvoice\n          note = \"placeholder\"\n          notes = [\"placeholder\"]\n", StringComparison.Ordinal) +
            "\n      event NoteRecorded\n        note Note\n";
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, options);
        _specification = _context.Specifications.Values.Single(_ => _.Name == "IssuingFirstInvoice");
        var command = _context.Commands[_specification.When.Command];
        _note = command.Properties.Single(_ => _.Name == "note");
        _notes = command.Properties.Single(_ => _.Name == "notes");
        _unusedEvent = _context.Events.Values.Single(_ => _.Name == "NoteRecorded");

        // Screenplay rejects null specification command values (PLAY0350) and empty array literals at binding,
        // so the placeholders are replaced here: this specification targets how Stage renders those values.
        _specification = WithCommandValue(_note.Id, SemanticValue.Null);
        _specification = WithCommandValue(_notes.Id, SemanticValue.Array([]));
    }

    void Because()
    {
        _withoutConstructors = SemanticCommandSpecificationRenderer.Render(_specification, _context).Content;
        _withCommandConstructor = SemanticCommandSpecificationRenderer.Render(WithCommandValue(_note.Id, SemanticValue.Text("note")), _context).Content;
        _withCollectionConstructor = SemanticCommandSpecificationRenderer.Render(WithCommandValue(_notes.Id, SemanticValue.Array([SemanticValue.Text("note")])), _context).Content;

        // Exercise the rendering seam independently of outcome admission: only an accepted branch emits the
        // expected-event constructor. The planner still rejects mixed success/error outcomes unchanged.
        var expected = _specification with
        {
            ThenEvents = [new(_unusedEvent.Id, [new(_unusedEvent.Properties.Single().Id, SemanticValue.Text("expected"))])]
        };
        _withExpectedConstructor = SemanticCommandSpecificationRenderer.Render(expected, _context).Content;
        _rejected = SemanticCommandSpecificationRenderer.Render(expected with { ThenErrors = [new(null, "rejected")] }, _context).Content;
    }

    [Fact] void should_emit_null_and_empty_collection_command_arguments() => _withoutConstructors.ShouldContain("new IssueInvoice(null, [], \"First payload\", \"invoice-one\")");
    [Fact] void should_not_import_common_for_null_and_empty_collection_values() => _withoutConstructors.ShouldNotContain("using Invoices.Common;");
    [Fact] void should_import_common_for_a_command_concept_constructor() => _withCommandConstructor.ShouldContain("using Invoices.Common;");
    [Fact] void should_import_common_for_a_command_collection_element_constructor() => _withCollectionConstructor.ShouldContain("using Invoices.Common;");
    [Fact] void should_import_common_for_an_accepted_expected_event_constructor() => _withExpectedConstructor.ShouldContain("using Invoices.Common;");
    [Fact] void should_emit_the_accepted_expected_event_constructor() => _withExpectedConstructor.ShouldContain("@event.Note == new Note(\"expected\")");
    [Fact] void should_not_import_common_for_unemitted_rejected_expectations() => _rejected.ShouldNotContain("using Invoices.Common;");
    [Fact] void should_not_emit_appended_event_assertions_for_rejections() => _rejected.ShouldNotContain("ShouldHaveAppendedEvent");

    SemanticSpecification WithCommandValue(SemanticId property, SemanticValue value) => _specification with
    {
        When = _specification.When with
        {
            Values = [.. _specification.When.Values.Select(_ => _.TargetProperty == property ? _ with { Value = value } : _)]
        }
    };
}
