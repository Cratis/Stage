// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelSchema;

public class when_generating_the_schema : Specification
{
    byte[] _generated = [];

    void Because() => _generated = given.render_model_schema.Generate();

    [Fact] void should_match_the_saved_schema() => for_RenderModelJson.given.render_models.EqualBytes(File.ReadAllBytes(given.render_model_schema.FilePath("render-model.v1.schema.json")), _generated);

    [Theory]
    [InlineData("kind", null)]
    [InlineData("kind", "event")]
    [InlineData("status", null)]
    [InlineData("status", "Present")]
    void should_reject_null_or_incorrect_fixed_members(string member, string? value)
    {
        var document = JsonNode.Parse(RenderModelJson.Serialize(for_RenderModelJson.given.render_models.Basic()))!;
        var command = document["items"]!["commands"]![0]!;
        var target = member == "kind" ? command : command["provenance"]!;
        target[member] = value;
        Validate(document).ShouldBeFalse();
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("provenance")]
    [InlineData("reason")]
    void should_reject_null_non_nullable_computed_members(string member)
    {
        var document = JsonNode.Parse(RenderModelJson.Serialize(for_RenderModelJson.given.render_models.Basic()))!;
        var command = document["items"]!["commands"]![0]!;
        var target = member switch
        {
            "schema" => document,
            "provenance" => command,
            _ => command["provenance"]!
        };
        target[member] = null;
        Validate(document).ShouldBeFalse();
    }

    [Theory]
    [InlineData("nameWords")]
    [InlineData("commands")]
    void should_reject_null_array_items(string member)
    {
        var document = JsonNode.Parse(RenderModelJson.Serialize(for_RenderModelJson.given.render_models.Basic()))!;
        var commands = document["items"]!["commands"]!;
        var array = member == "commands" ? commands : commands[0]!["nameWords"]!;
        array[0] = null;
        Validate(document).ShouldBeFalse();
    }

    bool Validate(JsonNode document) => new given.strict_schema_validator(JsonNode.Parse(_generated)!).Validate(JsonSerializer.SerializeToElement(document));

    [Theory]
    [InlineData("response")]
    [InlineData("empty")]
    [InlineData("reaction")]
    void should_validate_representative_builder_models(string fixture)
    {
        var model = fixture switch
        {
            "response" => for_RenderModelJson.given.render_models.Basic(),
            "empty" => for_RenderModelJson.given.render_models.Empty(),
            _ => for_RenderModelJson.given.render_models.Reaction()
        };
        using var document = JsonDocument.Parse(RenderModelJson.Serialize(model));
        new given.strict_schema_validator(JsonNode.Parse(_generated)!).Validate(document.RootElement).ShouldBeTrue();
    }
}
