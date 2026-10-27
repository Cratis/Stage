// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_admitting_binding_conditions : Specification
{
    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_admit_a_non_string_path_without_throwing(string json) =>
        SceneGuards.CanRunBinding(new JsonObject { ["path"] = JsonNode.Parse(json) }).ShouldBeTrue();

    [Theory]
    [InlineData("42")]
    [InlineData("\"$guard\"")]
    [InlineData("[]")]
    [InlineData("null")]
    void should_admit_a_non_object_condition_without_throwing(string json) =>
        SceneGuards.CanRunBinding(JsonNode.Parse(json)).ShouldBeTrue();

    [Fact] void should_admit_an_absent_path() => SceneGuards.CanRunBinding(new JsonObject()).ShouldBeTrue();
    [Fact] void should_admit_an_ordinary_binding_path() => SceneGuards.CanRunBinding(new JsonObject { ["path"] = "item.status" }).ShouldBeTrue();
    [Fact] void should_refuse_a_legacy_guard_envelope() => SceneGuards.CanRunBinding(new JsonObject { ["path"] = "$guard" }).ShouldBeFalse();
}
