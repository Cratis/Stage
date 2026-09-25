// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ApplicationSet;

public class when_indexing_an_event_with_generations : Specification
{
    [Fact]
    void should_reject_instead_of_indexing_the_oldest_or_latest_event_shape()
    {
        var syntax = new ScreenplayCompiler().Parse("""
            module Billing
              feature Invoices
                slice Automation DetectOverdueInvoices
                  event InvoiceRegistered generation 1
                    oldValue String
                  event InvoiceRegistered generation 2
                    newValue String
            """,
            "invoice.play").Value!;

        var failure = Catch.Exception(() => _ = new ApplicationSet([syntax]));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-EVENT-001: Event 'InvoiceRegistered' declares generations");
    }
}
#endif
