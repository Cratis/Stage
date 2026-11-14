// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_inspecting_stored_schemas_for_exact_numbers : given.a_schema
{
    static bool Exact(string schema) => SemanticSchemas.HasOnlyExactNumbers(JsonNode.Parse(schema)!.AsObject());

    [Fact] void should_accept_decimal_formats() => Exact("{\"properties\":{\"a\":{\"type\":\"number\",\"format\":\"decimal\"}}}").ShouldBeTrue();
    [Fact] void should_accept_a_trailing_question_mark() => Exact("{\"properties\":{\"a\":{\"type\":[\"number\",\"null\"],\"format\":\"decimal?\"}}}").ShouldBeTrue();
    [Fact] void should_refuse_a_format_less_number() => Exact("{\"properties\":{\"a\":{\"type\":\"number\"}}}").ShouldBeFalse();
    [Fact] void should_refuse_a_mixed_schema() => Exact("{\"properties\":{\"a\":{\"type\":\"number\",\"format\":\"decimal\"},\"b\":{\"type\":\"number\"}}}").ShouldBeFalse();
    [Fact] void should_refuse_a_number_nested_in_an_array_item() => Exact("{\"properties\":{\"a\":{\"type\":\"array\",\"items\":{\"type\":\"number\"}}}}").ShouldBeFalse();
    [Fact] void should_refuse_a_number_in_an_any_of_branch() => Exact("{\"properties\":{\"a\":{\"anyOf\":[{\"type\":\"number\"},{\"type\":\"null\"}]}}}").ShouldBeFalse();
    [Fact] void should_ignore_whole_numbers() => Exact("{\"properties\":{\"a\":{\"type\":\"integer\"}}}").ShouldBeTrue();
}
