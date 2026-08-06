// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_it_declares_an_identifier : given.a_command_handler
{
    const string InvoiceId = "8a4d1f7e-3c2b-4a5d-9e6f-0b1c2d3e4f50";

    async Task Because() => await HandlerFor("invoiceId").Handle(ContextFor(
        $$"""
        {
            "invoiceId": "{{InvoiceId}}",
            "invoiceNumber": "INV-000001"
        }
        """));

    [Fact] void should_append_to_the_event_source_the_payload_identifies() => _eventSourceId.ShouldEqual(InvoiceId);
}
