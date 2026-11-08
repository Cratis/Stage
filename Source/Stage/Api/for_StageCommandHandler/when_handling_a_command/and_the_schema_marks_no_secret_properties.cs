// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_schema_marks_no_secret_properties : given.a_command_handler
{
    string _raw = string.Empty;

    async Task Because()
    {
        var result = await HandlerWith("{\"type\":\"object\",\"properties\":{\"invoiceId\":{},\"email\":{\"type\":\"string\",\"x-conceptAttributes\":{\"pii\":\"\"}}}}")
            .Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"email\":\"a@b.c\"}"));
        _raw = JsonSerializer.Serialize(result);
    }

    [Fact] void should_echo_the_payload_unchanged() => _raw.ShouldEqual("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"email\":\"a@b.c\"}");
}
