// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelSchema;

public class when_validating_the_closed_subset : Specification
{
    [Theory]
    [InlineData("{\"type\":\"integer\"}", "1", "1.5")]
    [InlineData("{\"properties\":{\"value\":{\"const\":1}}}", "{\"value\":1}", "{\"value\":2}")]
    [InlineData("{\"required\":[\"value\"]}", "{\"value\":null}", "{}")]
    [InlineData("{\"items\":{\"type\":\"integer\"}}", "[1]", "[\"x\"]")]
    [InlineData("{\"enum\":[\"a\",\"b\"]}", "\"a\"", "\"c\"")]
    [InlineData("{\"anyOf\":[{\"type\":\"integer\"},{\"type\":\"string\"}]}", "1", "true")]
    [InlineData("{\"oneOf\":[{\"type\":\"number\"},{\"type\":\"integer\"}]}", "1.5", "1")]
    [InlineData("{\"properties\":{\"target\":{\"type\":\"integer\"}},\"$ref\":\"#/properties/target\"}", "1", "\"x\"")]
    [InlineData("{\"properties\":{\"value\":true},\"additionalProperties\":false}", "{\"value\":1}", "{\"extra\":1}")]
    [InlineData("{\"additionalProperties\":{\"type\":\"integer\"}}", "{\"value\":1}", "{\"value\":\"x\"}")]
    [InlineData("{\"const\":\"a\"}", "\"a\"", "\"b\"")]
    void should_accept_the_valid_document_and_reject_a_single_keyword_violation(string schema, string valid, string invalid)
    {
        var validator = new given.strict_schema_validator(JsonNode.Parse(schema)!);
        using var accepted = JsonDocument.Parse(valid);
        using var rejected = JsonDocument.Parse(invalid);
        validator.Validate(accepted.RootElement).ShouldBeTrue();
        validator.Validate(rejected.RootElement).ShouldBeFalse();
    }

    [Theory]
    [InlineData("{\"minLength\":1}")]
    [InlineData("{\"anyOf\":[true,{\"pattern\":\"x\"}]}")]
    [InlineData("{\"properties\":{\"unused\":{\"format\":\"date\"}}}")]
    void should_throw_on_unsupported_keywords_even_in_unvisited_branches(string schema) => Catch.Exception(() => _ = new given.strict_schema_validator(JsonNode.Parse(schema)!)).ShouldBeOfExactType<given.unsupported_schema_keyword>();

    [Fact]
    void should_resolve_array_indices_and_escaped_reference_tokens()
    {
        var schema = JsonNode.Parse("""{"properties":{"a/b~c":{"anyOf":[{"type":"integer"}]}},"$ref":"#/properties/a~1b~0c/anyOf/0"}""")!;
        using var valid = JsonDocument.Parse("1");
        using var invalid = JsonDocument.Parse("false");
        var validator = new given.strict_schema_validator(schema);
        validator.Validate(valid.RootElement).ShouldBeTrue();
        validator.Validate(invalid.RootElement).ShouldBeFalse();
    }
}
