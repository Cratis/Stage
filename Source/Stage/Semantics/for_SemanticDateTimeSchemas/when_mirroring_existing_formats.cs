// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticDateTimeSchemas;

public class when_mirroring_existing_formats : Specification
{
    JsonObject _built = null!;
    JsonObject _stored = null!;
    JsonObject _schema = null!;

    void Establish()
    {
        _built = JsonNode.Parse("""
            {"type":"object","properties":{
              "exact":{"type":"string","format":"date-time"},
              "legacy":{"type":"object","properties":{"at":{"type":"string","format":"date-time"}}},
              "optional":{"anyOf":[{"type":"string","format":"date-time"},{"type":"null"}]}
            }}
            """)!.AsObject();
        _stored = JsonNode.Parse("""
            {"type":"object","properties":{
              "exact":{"type":"string"},
              "legacy":{"type":"object","properties":{"at":{"type":"string","format":"date-time"}}},
              "optional":{"type":"string"}
            }}
            """)!.AsObject();
    }

    void Because() => _schema = SemanticDateTimeSchemas.ForStorage(_built, _stored);

    [Fact] void should_keep_a_lossless_registration_lossless() => _schema["properties"]!["exact"]!.AsObject().ContainsKey("format").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_format() => _schema["properties"]!["legacy"]!["properties"]!["at"]!["format"]!.GetValue<string>().ShouldEqual("date-time");
    [Fact] void should_name_only_the_lossy_nested_property() => SemanticDateTimeSchemas.LossyProperties(_built, _stored).ShouldEqual(["legacy.at"]);
    [Fact] void should_refuse_unknown_storage() => SemanticDateTimeSchemas.LossyProperties(_built, null).Count().ShouldEqual(3);
    [Fact] void should_keep_an_optional_plain_string_lossless() => _schema["properties"]!["optional"]!["anyOf"]![0]!.AsObject().ContainsKey("format").ShouldBeFalse();
}
