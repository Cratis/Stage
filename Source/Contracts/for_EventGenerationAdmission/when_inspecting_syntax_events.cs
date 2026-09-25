// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventGenerationAdmission;

public class when_inspecting_syntax_events : Specification
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              event InvoiceRegistered generation 1
                oldValue String
              event InvoiceRegistered generation 2
                newValue String
        """;

    [Fact]
    void should_reject_both_generations_before_converting_the_old_shape()
    {
        var syntax = new ScreenplayCompiler().Parse(Source, "invoice.play").Value!;
        var failure = Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(syntax));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-EVENT-001: Event 'InvoiceRegistered' declares generations");
    }

    [Fact]
    void should_reject_multiple_unmarked_shapes_in_the_direct_converter()
    {
        var syntax = new ScreenplayCompiler().Parse(Source, "invoice.play").Value!;
        var declarations = syntax.Modules.Single().Features.Single().Slices.Single().Events
            .Select(@event => @event with { HasGenerationMarker = false });
        var failure = Catch.Exception(() => EventGenerationAdmission.EnsureSupported(declarations, "Billing.Invoices.RegisterInvoice"));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-EVENT-001: Event 'InvoiceRegistered'");
    }

    [Fact]
    void should_reject_a_single_marked_generation_in_the_direct_converter()
    {
        var syntax = new ScreenplayCompiler().Parse(Source, "invoice.play").Value!;
        var @event = syntax.Modules.Single().Features.Single().Slices.Single().Events.Last();
        var failure = Catch.Exception(() => EventConverter.Convert([@event], [], new SchemaSynthesizer(new Dictionary<string, Cratis.Screenplay.Syntax.ConceptSyntax>()), "Billing.Invoices.RegisterInvoice"));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-EVENT-001: Event 'InvoiceRegistered'");
    }
}
#endif
