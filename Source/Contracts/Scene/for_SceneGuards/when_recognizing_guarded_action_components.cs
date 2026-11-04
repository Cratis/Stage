// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_recognizing_guarded_action_components : Specification
{
    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_not_recognize_a_non_string_component_name_or_throw(string json)
    {
        var element = new JsonObject
        {
            ["componentName"] = JsonNode.Parse(json),
            ["properties"] = new JsonObject { ["alternatives"] = new JsonArray() }
        };
        SceneGuards.IsGuardedAction(element).ShouldBeFalse();
    }

    [Theory]
    [InlineData("core:guarded-action")]
    [InlineData("core:action")]
    void should_preserve_both_guarded_action_component_names(string componentName)
    {
        var element = new JsonObject
        {
            ["componentName"] = componentName,
            ["properties"] = new JsonObject { ["alternatives"] = new JsonArray() }
        };
        SceneGuards.IsGuardedAction(element).ShouldBeTrue();
    }
}
