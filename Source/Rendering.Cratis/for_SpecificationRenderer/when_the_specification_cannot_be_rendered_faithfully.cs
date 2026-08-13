// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_the_specification_cannot_be_rendered_faithfully : given.a_slice_with_specifications
{
    string? _withGiven;
    string? _withReadModels;
    string? _withAnotherCommand;
    string? _assertingNothing;
    string? _expectingBoth;
    string? _renderable;

    void Because()
    {
        _withGiven = SpecificationRenderer.Unrenderable(
            Specification("Seeding", given: [Event("InvoiceRegistered", ("invoiceNumber", "INV-1"))], when: When("RegisterInvoice"), then: [Event("InvoiceRegistered")]),
            _sliceSyntax);
        _withReadModels = SpecificationRenderer.Unrenderable(
            Specification(
                "Reading",
                when: When("RegisterInvoice"),
                then: [Event("InvoiceRegistered")],
                thenReadModels: [new SpecificationReadModelSyntax("InvoiceList", [], SourceLocation.Start)]),
            _sliceSyntax);
        _withAnotherCommand = SpecificationRenderer.Unrenderable(
            Specification("Elsewhere", when: When("CancelInvoice"), then: [Event("InvoiceRegistered")]), _sliceSyntax);
        _assertingNothing = SpecificationRenderer.Unrenderable(Specification("Nothing", when: When("RegisterInvoice")), _sliceSyntax);
        _expectingBoth = SpecificationRenderer.Unrenderable(
            Specification(
                "Both",
                when: When("RegisterInvoice"),
                then: [Event("InvoiceRegistered")],
                errors: [new(null, SourceLocation.Start)]),
            _sliceSyntax);
        _renderable = SpecificationRenderer.Unrenderable(
            Specification("Registering", when: When("RegisterInvoice"), then: [Event("InvoiceRegistered")]), _sliceSyntax);
    }

    // The document, not the target, is what makes this unrenderable — CommandScenario does support Given.
    [Fact] void should_decline_a_specification_that_seeds_prior_state() =>
        _withGiven.ShouldContain("the document does not say which event source those events belong to");
    [Fact] void should_decline_a_specification_stating_read_model_state() =>
        _withReadModels.ShouldContain("no assertion in the scenario family");
    [Fact] void should_decline_a_specification_exercising_another_slice_s_command() =>
        _withAnotherCommand.ShouldContain("which this slice does not declare");
    [Fact] void should_decline_a_specification_that_asserts_nothing() => _assertingNothing.ShouldEqual("it asserts nothing");

    // A rejected command appends nothing, so a document stating both contradicts itself. Rendering either half
    // would drop the other silently, which is the one thing this renderer is written not to do.
    [Fact] void should_decline_a_specification_expecting_both_a_rejection_and_events() =>
        _expectingBoth.ShouldContain("a rejected command appends nothing");
    [Fact] void should_render_one_that_states_only_what_it_exercises_and_expects() => _renderable.ShouldBeNull();
}
