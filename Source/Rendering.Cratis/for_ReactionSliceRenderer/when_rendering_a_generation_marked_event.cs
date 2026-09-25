// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer;

public class when_rendering_a_generation_marked_event : an_automation_slice
{
    [Fact]
    void should_reject_the_event_before_generating_a_reactor()
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
        var events = syntax.Modules.Single().Features.Single().Slices.Single().Events;
        var slice = _detectorSlice with { Slice = _detectorSlice.Slice with { Events = events } };

        var failure = Catch.Exception(() => new ReactionSliceRenderer().Render(slice, _applicationSet, "CratisApp"));

        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-EVENT-001: Event 'InvoiceRegistered'");
    }
}
