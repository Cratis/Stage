// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_filtering_opaque_schema_positions : Specification
{
    const string Sentinel = "opaque-test-sentinel";

    const string Schema = """
        {"type":"object","properties":{
          "credentials":{"type":"object"},
          "node":{"type":"object","properties":{"name":{"type":"string"},"child":{"type":"object"}}},
          "nodes":{"type":"array","items":{"type":"object"}},
          "choice":{"anyOf":[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"string"}]},
          "empty":{},
          "untyped":{"properties":{"name":{"type":"string"}}},
          "unknown":{"type":"unknown"},
          "composed":{"type":"object","properties":{"name":{"type":"string"}},"allOf":[{}]},
          "opaqueScalar":{"type":"object"}
        }}
        """;

    JsonElement _result;
    string _raw = string.Empty;

    void Because()
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>($$$"""
            {"credentials":{"password":"{{{Sentinel}}}"},"node":{"name":"known","child":{"password":"{{{Sentinel}}}"}},
             "nodes":[{"password":"{{{Sentinel}}}"}],"choice":{"name":"{{{Sentinel}}}"},"empty":"{{{Sentinel}}}",
             "untyped":{"name":"{{{Sentinel}}}"},"unknown":"{{{Sentinel}}}","composed":{"name":"{{{Sentinel}}}"},"opaqueScalar":"{{{Sentinel}}}"}
            """)!;
        var filtered = SensitiveValues.Payload(payload, Schema);
        _result = JsonSerializer.SerializeToElement(filtered);
        _raw = _result.GetRawText();
    }

    [Fact] void should_not_echo_any_opaque_value() => _raw.ShouldNotContain(Sentinel);
    [Fact] void should_omit_an_object_without_declared_members() => _result.TryGetProperty("credentials", out _).ShouldBeFalse();
    [Fact] void should_omit_an_opaque_descendant_without_an_explicit_secret_marker() => _result.GetProperty("node").TryGetProperty("child", out _).ShouldBeFalse();
    [Fact] void should_omit_a_collection_of_opaque_items() => _result.TryGetProperty("nodes", out _).ShouldBeFalse();
    [Fact] void should_omit_an_any_of_position() => _result.TryGetProperty("choice", out _).ShouldBeFalse();
    [Fact] void should_omit_an_empty_schema_position() => _result.TryGetProperty("empty", out _).ShouldBeFalse();
    [Fact] void should_omit_an_untyped_position_with_properties() => _result.TryGetProperty("untyped", out _).ShouldBeFalse();
    [Fact] void should_omit_an_unrecognized_type() => _result.TryGetProperty("unknown", out _).ShouldBeFalse();
    [Fact] void should_omit_a_composed_position_even_with_declared_properties() => _result.TryGetProperty("composed", out _).ShouldBeFalse();
    [Fact] void should_omit_a_scalar_at_an_opaque_position() => _result.TryGetProperty("opaqueScalar", out _).ShouldBeFalse();
    [Fact] void should_keep_the_declared_sibling_of_an_opaque_position() => _result.GetProperty("node").GetProperty("name").GetString().ShouldEqual("known");
}
