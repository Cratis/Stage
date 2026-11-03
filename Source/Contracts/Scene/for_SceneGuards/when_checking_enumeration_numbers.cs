// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_checking_enumeration_numbers : Specification
{
    static JsonObject Comparison(string metadata) => JsonNode.Parse("""
        {"kind":"comparison","left":{"subject":"item.status"},"operator":"Equal","right":"open",
        """ + metadata + "}")!.AsObject();

    [Theory]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1,0]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\",\"cancelled\"],\"enumerationNumbers\":[-1,4,90]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"]")]
    public void should_admit_valid_or_absent_numbers(string metadata) => SceneGuards.CanEvaluate((object)Comparison(metadata)).ShouldBeTrue();

    [Theory]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1,1]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1.5,0]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[\"1\",0]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":null")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":1")]
    [InlineData("\"enumerationNumbers\":[1,0]")]
    [InlineData("\"enumeration\":[\"open\",0],\"enumerationNumbers\":[1,0]")]
    [InlineData("\"enumeration\":null,\"enumerationNumbers\":[1,0]")]
    [InlineData("\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1e400,0]")]
    public void should_refuse_malformed_metadata_everywhere(string metadata)
    {
        var comparison = Comparison(metadata);
        SceneGuards.CanEvaluate((object)comparison).ShouldBeFalse();
        SceneGuards.CanRunAction(new JsonObject
        {
            ["alternatives"] = new JsonArray(new JsonObject { ["command"] = "Close", ["condition"] = comparison.DeepClone() }),
            ["otherwise"] = new JsonObject { ["outcome"] = "Execute", ["command"] = "Reopen" }
        }).ShouldBeFalse();
        foreach (var kind in new[] { "firstMatch", "otherwise" })
        {
            SceneGuards.CanRunBinding(new JsonObject
            {
                ["path"] = SceneGuards.GuardPath,
                ["value"] = new JsonObject { ["kind"] = kind, ["index"] = 0, ["alternatives"] = new JsonArray(comparison.DeepClone()) }
            }).ShouldBeFalse();
        }
        SceneGuards.CanEvaluate((object)new JsonObject
        {
            ["kind"] = "logical", ["operator"] = "Or", ["left"] = comparison.DeepClone(),
            ["right"] = Comparison("\"enumeration\":[\"open\",\"closed\"]")
        }).ShouldBeFalse();
    }
}
