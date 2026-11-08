// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_schema_marks_secret_properties : given.a_command_handler
{
    const string SecretValue = "s3cret-value";

    const string Schema = """
        {"type":"object","properties":{
          "invoiceId":{},
          "note":{"type":"string"},
          "password":{"type":"string","x-conceptAttributes":{"sensitive":""}},
          "pins":{"type":"array","items":{"type":"string","x-conceptAttributes":{"sensitive":""}}},
          "credentials":{"type":"object","properties":{"note":{"type":"string"},"password":{"type":"string","x-conceptAttributes":{"sensitive":""}}}},
          "history":{"type":"array","items":{"type":"object","properties":{"note":{"type":"string"},"password":{"type":"string","x-conceptAttributes":{"sensitive":""}}}}}
        }}
        """;

    JsonElement _response;
    string _raw = string.Empty;

    async Task Because()
    {
        var result = await HandlerWith(Schema).Handle(ContextFor(
            $"{{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"note\":\"hello\",\"password\":\"{SecretValue}\",\"pins\":[\"{SecretValue}\"],\"credentials\":{{\"note\":\"main\",\"password\":\"{SecretValue}\"}},\"history\":[{{\"note\":\"old\",\"password\":\"{SecretValue}\"}}]}}"));
        _raw = JsonSerializer.Serialize(result);
        _response = JsonSerializer.Deserialize<JsonElement>(_raw);
    }

    [Fact] void should_not_contain_the_secret_value_anywhere() => _raw.ShouldNotContain(SecretValue);
    [Fact] void should_omit_the_secret_property() => _response.TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_omit_a_list_of_secrets() => _response.TryGetProperty("pins", out _).ShouldBeFalse();
    [Fact] void should_omit_the_secret_inside_a_child_object() => _response.GetProperty("credentials").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_omit_the_secret_inside_a_list_of_objects() => _response.GetProperty("history")[0].TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_non_secret_properties() => _response.GetProperty("note").GetString().ShouldEqual("hello");
    [Fact] void should_keep_the_non_secret_child_properties() => _response.GetProperty("credentials").GetProperty("note").GetString().ShouldEqual("main");
}
