// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_schema_cuts_a_recursive_type : given.a_command_handler
{
    const string Schema = """
        {"type":"object","properties":{
          "invoiceId":{},
          "password":{"type":"string","x-conceptAttributes":{"sensitive":""}},
          "node":{"type":"object","properties":{"name":{"type":"string"},"child":{"type":"object"}}},
          "nodes":{"type":"array","items":{"type":"object"}}
        }}
        """;

    JsonElement _response;

    async Task Because()
    {
        var result = await HandlerWith(Schema).Handle(ContextFor(
            "{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"password\":\"s3cret-value\",\"node\":{\"name\":\"a\",\"child\":{\"password\":\"s3cret-value\"}},\"nodes\":[{\"password\":\"s3cret-value\"}]}"));
        _response = JsonSerializer.SerializeToElement(result);
    }

    [Fact] void should_drop_the_object_whose_schema_was_cut() => _response.GetProperty("node").TryGetProperty("child", out _).ShouldBeFalse();
    [Fact] void should_drop_a_list_of_objects_whose_schema_was_cut() => _response.TryGetProperty("nodes", out _).ShouldBeFalse();
    [Fact] void should_keep_the_known_members_of_the_enclosing_object() => _response.GetProperty("node").GetProperty("name").GetString().ShouldEqual("a");
}
