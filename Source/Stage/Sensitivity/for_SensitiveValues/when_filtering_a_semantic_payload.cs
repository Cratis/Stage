// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_filtering_a_semantic_payload : given.a_semantic_application
{
    JsonElement _result;
    string _raw = string.Empty;

    void Because()
    {
        var secret = $"\"password\":\"{SecretValue}\"";
        var filtered = SensitiveValues.Payload(
            Payload($"{{\"projectId\":\"p\",\"PASSWORD\":\"{SecretValue}\",\"credentials\":{{\"note\":\"main\",{secret}}},\"wrapper\":{{\"label\":\"w\",\"inner\":{{\"note\":\"deep\",{secret}}}}},\"history\":[{{\"note\":\"old\",{secret}}}],\"node\":{{\"name\":\"a\",{secret},\"child\":{{\"name\":\"b\",{secret}}}}}}}"),
            _echo.Properties,
            _application);
        _raw = Json(filtered);
        _result = JsonSerializer.SerializeToElement(filtered);
    }

    [Fact] void should_not_contain_the_secret_value_anywhere() => _raw.ShouldNotContain(SecretValue);
    [Fact] void should_omit_a_top_level_secret_whatever_its_casing() => _result.EnumerateObject().Any(member => string.Equals(member.Name, "password", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_keep_the_ordinary_top_level_values() => _result.GetProperty("projectId").GetString().ShouldEqual("p");
    [Fact] void should_omit_a_secret_in_a_nested_composite() => _result.GetProperty("credentials").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_members_of_a_nested_composite() => _result.GetProperty("credentials").GetProperty("note").GetString().ShouldEqual("main");
    [Fact] void should_omit_a_secret_two_composites_deep() => _result.GetProperty("wrapper").GetProperty("inner").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_omit_a_secret_in_a_list_of_composites() => _result.GetProperty("history")[0].TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_members_in_a_list_of_composites() => _result.GetProperty("history")[0].GetProperty("note").GetString().ShouldEqual("old");
    [Fact] void should_omit_a_secret_in_a_recursive_type() => _result.GetProperty("node").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_drop_what_the_recursion_cut_cannot_be_seen_into() => _result.GetProperty("node").TryGetProperty("child", out _).ShouldBeFalse();
}
