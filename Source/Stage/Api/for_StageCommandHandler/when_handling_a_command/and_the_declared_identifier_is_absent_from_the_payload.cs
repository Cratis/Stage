// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_declared_identifier_is_absent_from_the_payload : given.a_command_handler
{
    async Task Because() => await HandlerFor("invoiceId").Handle(ContextFor("""{ "invoiceNumber": "INV-000001" }"""));

    [Fact] void should_append_to_a_generated_event_source() => Guid.TryParse(_eventSourceId, out _).ShouldBeTrue();
}
