// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

/// <summary>
/// Rendering a spec that reads well and does not compile is the failure mode that matters here, so the slice it
/// exercises is rendered alongside it and the pair is compiled against the real Cratis testing assemblies.
/// </summary>
public class when_rendering_a_specification : given.a_slice_with_specifications
{
    RenderedFile _appended = null!;
    RenderedFile _rejected = null!;
    IReadOnlyList<string> _errors = null!;

    void Because()
    {
        _appended = SpecificationRenderer.Render(
            Specification(
                "RegisteringAnInvoice",
                when: When("RegisterInvoice", ("invoiceId", "9c858901-8a57-4791-81fe-4c455b099bc9"), ("invoiceNumber", "INV-000123")),
                then: [Event("InvoiceRegistered", ("invoiceNumber", "INV-000123"))]),
            _command,
            _slice,
            _applicationSet,
            "Acme");

        _rejected = SpecificationRenderer.Render(
            Specification(
                "RejectingAnInvoiceWithNoNumber",
                when: When("RegisterInvoice", ("invoiceId", "9c858901-8a57-4791-81fe-4c455b099bc9")),
                errors: [new(null, Screenplay.Diagnostics.SourceLocation.Start)]),
            _command,
            _slice,
            _applicationSet,
            "Acme");

        var slice = new StateChangeSliceRenderer().Render(_slice, _applicationSet, "Acme");
        var concepts = _applicationSet.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, _applicationSet, "Acme"));
        _errors = RenderedOutput.Errors([slice, _appended, _rejected, .. concepts]);
    }

    [Fact] void should_render_specs_that_compile() => _errors.ShouldBeEmpty();
    [Fact] void should_name_the_file_for_the_behavior() =>
        _appended.RelativePath.EndsWith("when_registering_an_invoice.cs", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_ship_only_in_debug() => _appended.Content.StartsWith("#if DEBUG", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_exercise_the_command_through_a_scenario() =>
        _appended.Content.ShouldContain("readonly CommandScenario<RegisterInvoice> _scenario = new();");
    [Fact] void should_state_the_uuid_the_document_wrote_as_text() =>
        _appended.Content.ShouldContain("Guid.Parse(\"9c858901-8a57-4791-81fe-4c455b099bc9\")");
    [Fact] void should_assert_the_appended_event_against_its_event_source() =>
        _appended.Content.ShouldContain(
            "await _scenario.ShouldHaveAppendedEvent<RegisterInvoice, InvoiceRegistered>(new EventSourceId(\"9c858901-8a57-4791-81fe-4c455b099bc9\"), @event => @event.InvoiceNumber == \"INV-000123\");");

    // Both, deliberately: on its own ShouldNotBeSuccessful cannot tell a rejection from an unhandled exception.
    [Fact] void should_assert_a_rejection_as_both_unsuccessful_and_invalid() =>
        _rejected.Content.ShouldContain("_result.ShouldNotBeSuccessful();");
    [Fact] void should_assert_a_rejection_has_validation_errors() =>
        _rejected.Content.ShouldContain("_result.ShouldHaveValidationErrors();");
    [Fact] void should_report_what_the_document_left_unstated() =>
        _rejected.Diagnostics.ShouldContain(
            "Specification 'RejectingAnInvoiceWithNoNumber' states no value for 'invoiceNumber' of command 'RegisterInvoice' — the rendered spec constructs them as missing values.");
}
