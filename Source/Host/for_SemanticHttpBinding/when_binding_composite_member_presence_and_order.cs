// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_binding_composite_member_presence_and_order : a_routed_model
{
    readonly List<(int Status, JsonElement Body)> _responses = [];

    void Establish()
    {
        var plan = compiled_plan.From(Source);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan, appender);
    }

    async Task Because()
    {
        foreach (var value in new[] { "{}", "{\"first\":null}", "{\"second\":null,\"first\":null}" })
        {
            var response = await Request("POST", "/api/inputs/checking/check/check-input", $"{{\"id\":\"one\",\"details\":{value}}}");
            _responses.Add((response.Status, JsonSerializer.Deserialize<JsonElement>(response.Body)));
        }
    }

    [Fact] void should_not_validate_omitted_nested_concepts() => _responses[0].Status.ShouldEqual(200);
    [Fact] void should_validate_explicit_null_nested_concepts() => _responses[1].Status.ShouldEqual(400);
    [Fact] void should_only_fail_the_explicitly_supplied_null() => Assert.Equal(["First required"], Messages(_responses[1].Body));
    [Fact] void should_report_failures_in_supplied_member_order() => Assert.Equal(["Second required", "First required"], Messages(_responses[2].Body));

    static string?[] Messages(JsonElement body) => [.. body.GetProperty("validationResults").EnumerateArray().Select(failure => failure.GetProperty("message").GetString())];

    const string Source = """
        concept First : String
          validate
            not empty message "First required"
        concept Second : String
          validate
            not empty message "Second required"
        type Details
          first First optional
          second Second optional
        module Inputs
          feature Checking
            slice StateChange Check
              command CheckInput
                id String
                details Details
                returns id
        """;
}
