// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_sourceless_given_event_and_a_projection : a_projected_specification
{
    void Establish()
    {
        var plan = Compile("""
            specification RegisteringAfterASourcelessInvoice
              given InvoiceRegistered
                description = "Seeded"
              when append InvoiceRegistered
                for "second"
                description = "North"
              then readmodel InvoiceView
                invoiceId = "second"
                description = "North"
            """);
        var specification = plan.Specifications.Values.Single();
        _plan = plan;
        _sourceless = specification.GivenEvents.All(given => given.EventSource is null);
    }

    bool _sourceless;

    Task Because() => Run();

    [Fact] void should_have_no_event_source_on_the_given_event() => _sourceless.ShouldBeTrue();
    [Fact] void should_still_refuse_the_projection_that_needs_a_typed_source() => _result.Unsupported.ShouldNotBeNull();
    [Fact] void should_not_pass() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
}
