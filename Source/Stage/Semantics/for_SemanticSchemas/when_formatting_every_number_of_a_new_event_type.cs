// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_formatting_every_number_of_a_new_event_type : given.a_schema
{
    JsonObject _schema = null!;

    void Because() => _schema = SemanticSchemas.WithDecimalFormats(
        JsonNode.Parse(Schema("concept Price : Decimal\ntype Line\n  amount Decimal\n  count Int", "Line[]"))!.AsObject(), null);

    [Fact] void should_format_the_collection_item_member() => _schema["properties"]!["value"]!["items"]!["properties"]!["amount"]!["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_leave_a_whole_number_alone() => _schema["properties"]!["value"]!["items"]!["properties"]!["count"]!.AsObject().ContainsKey("format").ShouldBeFalse();
}
