// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_mirroring_a_mixed_stored_schema : given.a_schema
{
    const string Stored = "{\"type\":\"object\",\"properties\":{\"exact\":{\"type\":\"number\",\"format\":\"decimal\"},\"legacy\":{\"type\":\"number\"}}}";

    JsonObject _schema = null!;

    void Because() => _schema = SemanticSchemas.WithDecimalFormats(
        JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"exact\":{\"type\":\"number\"},\"legacy\":{\"type\":\"number\"},\"added\":{\"type\":\"number\"}}}")!.AsObject(),
        JsonNode.Parse(Stored)!.AsObject());

    [Fact] void should_keep_a_stored_decimal_exact() => _schema["properties"]!["exact"]!["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_keep_a_stored_format_less_number_format_less() => _schema["properties"]!["legacy"]!.AsObject().ContainsKey("format").ShouldBeFalse();
    [Fact] void should_not_format_a_property_the_stored_schema_lacks() => _schema["properties"]!["added"]!.AsObject().ContainsKey("format").ShouldBeFalse();
}
