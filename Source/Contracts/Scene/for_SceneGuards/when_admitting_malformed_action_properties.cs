// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_admitting_malformed_action_properties : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    JsonObject _properties;

    void Establish() => _properties = JsonSerializer.SerializeToNode(
        new
        {
            alternatives = new[]
            {
                new
                {
                    command = "CloseWorkItem",
                    condition = new { kind = "comparison", left = SceneGuards.Subject("item.status"), @operator = "Equal", right = "open" }
                }
            }
        },
        _options)!.AsObject();

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_refuse_a_non_string_command_without_throwing(string json)
    {
        _properties["alternatives"]![0]!["command"] = JsonNode.Parse(json);
        SceneGuards.CanRunAction(_properties).ShouldBeFalse();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"branch\"")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_refuse_a_non_object_branch_without_throwing(string json)
    {
        _properties["alternatives"]![0] = JsonNode.Parse(json);
        SceneGuards.CanRunAction(_properties).ShouldBeFalse();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"Hidden\"")]
    [InlineData("[]")]
    void should_refuse_a_non_object_otherwise_without_throwing(string json)
    {
        _properties["otherwise"] = JsonNode.Parse(json);
        SceneGuards.CanRunAction(_properties).ShouldBeFalse();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_refuse_a_non_string_outcome_without_throwing(string json)
    {
        _properties["otherwise"] = new JsonObject { ["outcome"] = JsonNode.Parse(json), ["command"] = "CloseWorkItem" };
        SceneGuards.CanRunAction(_properties).ShouldBeFalse();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("{}")]
    [InlineData("[]")]
    void should_refuse_a_non_string_fallback_command_without_throwing(string json)
    {
        _properties["otherwise"] = new JsonObject { ["outcome"] = "Execute", ["command"] = JsonNode.Parse(json) };
        SceneGuards.CanRunAction(_properties).ShouldBeFalse();
    }

    [Fact] void should_admit_valid_alternatives_without_a_fallback() => SceneGuards.CanRunAction(_properties).ShouldBeTrue();

    [Theory]
    [InlineData("null")]
    [InlineData("{\"outcome\":\"Hidden\"}")]
    [InlineData("{\"outcome\":\"Execute\",\"command\":\"CloseWorkItem\"}")]
    void should_preserve_valid_fallback_admission(string json)
    {
        _properties["otherwise"] = JsonNode.Parse(json);
        SceneGuards.CanRunAction(_properties).ShouldBeTrue();
    }
}
