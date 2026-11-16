// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_formatting_scalars_and_optionals_of_a_new_event_type : given.a_schema
{
    JsonObject _scalar = null!;
    JsonObject _concept = null!;
    JsonObject _optional = null!;
    JsonObject _optionalConcept = null!;
    JsonObject _dateTime = null!;

    void Because()
    {
        _scalar = Format(Schema("", "Decimal"));
        _concept = Format(Schema("concept Price : Decimal", "Price"));
        _optional = Format(Schema("", "Decimal?"));
        _optionalConcept = Format(Schema("concept Price : Decimal pii", "Price?"));
        _dateTime = Format(Schema("", "DateTime"));
    }

    static JsonObject Format(string schema) => SemanticSchemas.WithDecimalFormats(JsonNode.Parse(schema)!.AsObject(), null)["properties"]!["value"]!.AsObject();

    [Fact] void should_format_a_decimal() => _scalar["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_format_a_decimal_concept() => _concept["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_format_the_number_branch_of_an_optional() => _optional["anyOf"]![0]!["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_not_format_the_null_branch_of_an_optional() => _optional["anyOf"]![1]!.AsObject().ContainsKey("format").ShouldBeFalse();
    [Fact] void should_format_a_nullable_type_array() => _optionalConcept["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_leave_a_date_time_with_its_own_format() => _dateTime["format"]!.GetValue<string>().ShouldEqual("date-time");
}
