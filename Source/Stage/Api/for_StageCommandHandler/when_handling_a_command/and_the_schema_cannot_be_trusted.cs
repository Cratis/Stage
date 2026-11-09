// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_schema_cannot_be_trusted : given.a_command_handler
{
    const string Marked = "{\"type\":\"string\",\"x-conceptAttributes\":{\"sensitive\":\"\"}}";

    string _unreadable = string.Empty;
    string _mistyped = string.Empty;

    async Task Because()
    {
        _unreadable = JsonSerializer.Serialize(await HandlerWith("{not json")
            .Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"password\":\"s3cret-value\"}")));
        _mistyped = JsonSerializer.Serialize(await HandlerWith($"{{\"type\":\"object\",\"properties\":{{\"invoiceId\":{{}},\"credentials\":{{\"type\":\"object\",\"properties\":{{\"password\":{Marked}}}}},\"pins\":{{\"type\":\"array\",\"items\":{Marked}}},\"note\":{{\"type\":\"string\"}}}}}}")
            .Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"credentials\":[\"s3cret-value\"],\"pins\":\"s3cret-value\",\"note\":\"hello\"}")));
    }

    [Fact] void should_echo_nothing_for_an_unreadable_schema() => _unreadable.ShouldEqual("{}");
    [Fact] void should_not_echo_a_value_of_the_wrong_shape_where_secrets_are_held() => _mistyped.ShouldNotContain("s3cret-value");
    [Fact] void should_still_echo_other_properties() => _mistyped.ShouldContain("hello");
}
