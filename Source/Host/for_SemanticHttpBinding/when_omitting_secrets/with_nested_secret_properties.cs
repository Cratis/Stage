// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class with_nested_secret_properties : given.a_secret_application
{
    JsonElement _response;

    async Task Because()
    {
        await Post("enroll", $"{{\"username\":\"u\",\"credentials\":{{\"note\":\"main\",\"password\":\"{SecretValue}\"}},\"history\":[{{\"note\":\"old\",\"password\":\"{SecretValue}\"}}],\"pins\":[\"{SecretValue}\"]}}");
        _response = _result.GetProperty("response");
    }

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_not_contain_the_secret_value_anywhere() => _body.ShouldNotContain(SecretValue);
    [Fact] void should_omit_the_secret_inside_a_child_object() => _response.GetProperty("credentials").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_child_properties() => _response.GetProperty("credentials").GetProperty("note").GetString().ShouldEqual("main");
    [Fact] void should_omit_the_secret_inside_a_list_of_objects() => _response.GetProperty("history")[0].TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_properties_inside_a_list() => _response.GetProperty("history")[0].GetProperty("note").GetString().ShouldEqual("old");
    [Fact] void should_omit_a_list_of_secrets() => _response.TryGetProperty("pins", out _).ShouldBeFalse();
    [Fact] void should_still_return_the_top_level_properties() => _response.GetProperty("username").GetString().ShouldEqual("u");
}
