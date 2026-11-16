// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_mirroring_stored_optional_shapes : given.a_schema
{
    JsonObject _anyOfOverTypeArray = null!;
    JsonObject _typeArrayOverAnyOf = null!;

    void Because()
    {
        _anyOfOverTypeArray = SemanticSchemas.WithDecimalFormats(
            JsonNode.Parse("{\"properties\":{\"value\":{\"anyOf\":[{\"type\":\"number\"},{\"type\":\"null\"}]}}}")!.AsObject(),
            JsonNode.Parse("{\"properties\":{\"value\":{\"type\":[\"number\",\"null\"],\"format\":\"decimal?\"}}}")!.AsObject());
        _typeArrayOverAnyOf = SemanticSchemas.WithDecimalFormats(
            JsonNode.Parse("{\"properties\":{\"value\":{\"type\":[\"number\",\"null\"]}}}")!.AsObject(),
            JsonNode.Parse("{\"properties\":{\"value\":{\"anyOf\":[{\"type\":\"number\",\"format\":\"decimal\"},{\"type\":\"null\"}]}}}")!.AsObject());
    }

    [Fact] void should_copy_the_stored_nullable_format_onto_the_any_of_branch() => _anyOfOverTypeArray["properties"]!["value"]!["anyOf"]![0]!["format"]!.GetValue<string>().ShouldEqual("decimal?");
    [Fact] void should_copy_the_stored_branch_format_onto_the_type_array() => _typeArrayOverAnyOf["properties"]!["value"]!["format"]!.GetValue<string>().ShouldEqual("decimal");
}
