// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_filtering_undeclared_schema_members : Specification
{
    const string Schema = """
        {"type":"object","properties":{
          "name":{"type":"string"},
          "address":{"type":"object","properties":{"city":{"type":"string"}}},
          "history":{"type":"array","items":{"type":"object","properties":{"note":{"type":"string"}}}},
          "tags":{"type":"array","items":{"type":"string"}},
          "active":{"type":"boolean"},"count":{"type":"integer"},"amount":{"type":"number"},"nothing":{"type":"null"}
        }}
        """;

    JsonElement _result;

    void Because() => _result = JsonSerializer.SerializeToElement(SensitiveValues.Payload(
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
            {"NAME":"known","extra":"undeclared-test-sentinel","address":{"city":"Oslo","extra":"undeclared-test-sentinel"},
             "history":[{"note":"old","extra":"undeclared-test-sentinel"}],"tags":["one","two"],"active":true,"count":2,"amount":1.5,"nothing":null}
            """)!,
        Schema));

    [Fact] void should_omit_undeclared_values_at_every_depth() => _result.GetRawText().ShouldNotContain("undeclared-test-sentinel");
    [Fact] void should_omit_an_undeclared_top_level_member() => _result.TryGetProperty("extra", out _).ShouldBeFalse();
    [Fact] void should_omit_an_undeclared_nested_member() => _result.GetProperty("address").TryGetProperty("extra", out _).ShouldBeFalse();
    [Fact] void should_omit_an_undeclared_array_item_member() => _result.GetProperty("history")[0].TryGetProperty("extra", out _).ShouldBeFalse();
    [Fact] void should_preserve_declared_values_and_the_callers_key_casing() => _result.GetRawText().ShouldEqual("""
        {"NAME":"known","address":{"city":"Oslo"},"history":[{"note":"old"}],"tags":["one","two"],"active":true,"count":2,"amount":1.5,"nothing":null}
        """);
}
