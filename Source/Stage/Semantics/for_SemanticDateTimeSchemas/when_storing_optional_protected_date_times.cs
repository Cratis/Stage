// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Semantics.for_SemanticSchemas.given;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticDateTimeSchemas;

public class when_storing_optional_protected_date_times : a_schema
{
    [Theory]
    [InlineData("pii", "compliance")]
    [InlineData("secret", "security")]
    public void should_register_a_lossless_nullable_string_and_keep_protection(string protection, string metadata)
    {
        var built = JsonNode.Parse(Schema($"concept Protected : DateTime {protection}", "Protected?"))!.AsObject();
        var storage = SemanticDateTimeSchemas.ForStorage(built, null);
        var property = storage["properties"]!["value"]!.AsObject();

        property["type"]!.ToJsonString().ShouldEqual("[\"string\",\"null\"]");
        property.ContainsKey("format").ShouldBeFalse();
        property[metadata]!.ToJsonString().ShouldEqual(built["properties"]!["value"]![metadata]!.ToJsonString());
        SemanticDateTimeSchemas.LossyProperties(built, storage).ShouldBeEmpty();
        SemanticDateTimeSchemas.ForStorage(built, storage).ToJsonString().ShouldEqual(storage.ToJsonString());
    }

    [Fact]
    public void should_preserve_and_identify_a_legacy_nullable_pii_date_time()
    {
        var built = JsonNode.Parse(Schema("concept Protected : DateTime pii", "Protected?"))!.AsObject();

        JsonNode.DeepEquals(SemanticDateTimeSchemas.ForStorage(built, built), built).ShouldBeTrue();
        SemanticDateTimeSchemas.LossyProperties(built, built).ShouldEqual(["value"]);
    }

    [Fact]
    public void should_detect_nullable_type_arrays_inside_optional_branches()
    {
        var built = JsonNode.Parse("""
            {"type":"object","properties":{"value":{"anyOf":[
              {"type":["string","null"],"format":"date-time","compliance":[]},
              {"type":"null"}
            ]}}}
            """)!.AsObject();
        var storage = SemanticDateTimeSchemas.ForStorage(built, null);

        SemanticDateTimeSchemas.LossyProperties(built, storage).ShouldBeEmpty();
        SemanticDateTimeSchemas.LossyProperties(built, built).ShouldEqual(["value"]);
    }
}
