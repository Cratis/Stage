// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_converting_empty_rows : Specification
{
    ApplicationSyntax _syntax;
    JsonNode _result;

    void Establish()
    {
        var at = SourceLocation.Start;
        var field = new SampleDataFieldSyntax("lines", SampleDataValueKind.Rows, null, at) { Rows = [] };
        _syntax = new([], [], [], [], at)
        {
            SampleDataPacks = [new("OrdersDemo", null, null, [new("Order", [new([field], at)], at)], at)]
        };
    }

    void Because() => _result = SampleDataConverter.Packs(_syntax).Single().Sets.Single().Rows.Single()["lines"];

    [Fact] void should_convert_to_an_array() => _result.ShouldBeOfExactType<JsonArray>();
    [Fact] void should_keep_the_array_empty() => _result.AsArray().ShouldBeEmpty();
    [Fact] void should_serialize_as_an_empty_array() => _result.ToJsonString().ShouldEqual("[]");
}
