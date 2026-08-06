// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_it_declares_no_identifier : given.a_command_handler
{
    const string InvoiceId = "8a4d1f7e-3c2b-4a5d-9e6f-0b1c2d3e4f50";

    const string Payload =
        $$"""
        {
            "invoiceId": "{{InvoiceId}}",
            "invoiceNumber": "INV-000001"
        }
        """;

    string _first = string.Empty;

    async Task Because()
    {
        await HandlerFor(null).Handle(ContextFor(Payload));
        _first = _eventSourceId;
        await HandlerFor(null).Handle(ContextFor(Payload));
    }

    [Fact] void should_append_to_a_generated_event_source() => Guid.TryParse(_eventSourceId, out _).ShouldBeTrue();
    [Fact] void should_not_take_a_value_off_the_payload() => _eventSourceId.ShouldNotEqual(InvoiceId);
    [Fact] void should_open_a_new_event_source_for_every_execution() => _eventSourceId.ShouldNotEqual(_first);
}
