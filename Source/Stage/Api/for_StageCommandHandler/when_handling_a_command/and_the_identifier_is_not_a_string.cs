// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_identifier_is_not_a_string : given.a_command_handler
{
    async Task Because() => await HandlerFor("invoiceNumber").Handle(ContextFor("""{ "invoiceNumber": 4711 }"""));

    [Fact] void should_append_to_the_event_source_the_value_renders_as() => _eventSourceId.ShouldEqual("4711");
}
