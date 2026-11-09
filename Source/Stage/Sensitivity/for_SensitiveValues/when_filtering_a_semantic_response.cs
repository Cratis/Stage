// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_filtering_a_semantic_response : given.a_semantic_application
{
    JsonElement _scalarResult;
    JsonElement _recordResult;
    object? _untouched;
    readonly Dictionary<string, object?> _plain = new() { ["name"] = "n" };

    void Because()
    {
        var credentials = new Dictionary<string, object?> { ["note"] = "main", ["password"] = SecretValue };
        _scalarResult = JsonSerializer.SerializeToElement(SensitiveValues.Response(credentials, _scalar.Response, _application));
        _recordResult = JsonSerializer.SerializeToElement(SensitiveValues.Response(
            new Dictionary<string, object?> { ["name"] = "n", ["credentials"] = credentials },
            _record.Response,
            _application));
        _untouched = SensitiveValues.Response(_plain, null, _application);
    }

    [Fact] void should_omit_the_secret_of_a_composite_return() => _scalarResult.TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_members_of_a_composite_return() => _scalarResult.GetProperty("note").GetString().ShouldEqual("main");
    [Fact] void should_omit_the_secret_of_a_composite_record_field() => _recordResult.GetProperty("credentials").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_keep_the_other_members_of_a_composite_record_field() => _recordResult.GetProperty("credentials").GetProperty("note").GetString().ShouldEqual("main");
    [Fact] void should_keep_the_other_record_fields() => _recordResult.GetProperty("name").GetString().ShouldEqual("n");
    [Fact] void should_not_contain_the_secret_value_anywhere() => (_scalarResult.GetRawText() + _recordResult.GetRawText()).ShouldNotContain(SecretValue);
    [Fact] void should_return_a_response_without_secrets_as_it_is() => ReferenceEquals(_untouched, _plain).ShouldBeTrue();
}
