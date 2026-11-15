// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticDateTimeSchemas;

public class when_registering_a_new_event_type : Specification
{
    const string Shape = """
        {"type":"object","properties":{
          "at":{"type":"string","format":"date-time"},
          "optional":{"anyOf":[{"type":"string","format":"date-time"},{"type":"null"}]},
          "many":{"type":"array","items":{"type":"object","properties":{"at":{"type":"string","format":"date-time"}}}},
          "amount":{"type":"number","format":"decimal"}
        }}
        """;
    JsonObject _built = null!;
    JsonObject _schema = null!;

    void Establish() => _built = JsonNode.Parse(Shape)!.AsObject();
    void Because() => _schema = SemanticDateTimeSchemas.ForStorage(_built, null);

    [Fact] void should_use_a_plain_string() => _schema["properties"]!["at"]!.ToJsonString().ShouldEqual("{\"type\":\"string\"}");
    [Fact] void should_preserve_optional_shape() => _schema["properties"]!["optional"]!["anyOf"]![1]!["type"]!.GetValue<string>().ShouldEqual("null");
    [Fact] void should_keep_every_date_time_lossless() => SemanticDateTimeSchemas.LossyProperties(_built, _schema).ShouldBeEmpty();
    [Fact] void should_not_change_the_read_model_schema() => _built["properties"]!["at"]!["format"]!.GetValue<string>().ShouldEqual("date-time");
    [Fact] void should_keep_decimal_formats() => _schema["properties"]!["amount"]!["format"]!.GetValue<string>().ShouldEqual("decimal");
}
