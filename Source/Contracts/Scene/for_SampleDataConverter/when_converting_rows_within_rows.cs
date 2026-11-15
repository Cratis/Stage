// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_converting_rows_within_rows : Specification
{
    ApplicationSyntax _syntax;
    JsonObject _result;

    void Establish()
    {
        var at = SourceLocation.Start;
        var discounts = new SampleDataFieldSyntax("discounts", SampleDataValueKind.Rows, null, at)
        {
            Rows =
            [
                new([new("amount", SampleDataValueKind.Number, "1.5", at)], at),
                new([new("amount", SampleDataValueKind.Number, "2.5", at)], at)
            ]
        };
        var lines = new SampleDataFieldSyntax("lines", SampleDataValueKind.Rows, null, at)
        {
            Rows = [new([new("product", SampleDataValueKind.Text, "Notebook", at), discounts], at)]
        };
        _syntax = new([], [], [], [], at)
        {
            SampleDataPacks = [new("OrdersDemo", null, null, [new("Order", [new([lines], at)], at)], at)]
        };
    }

    void Because() => _result = SampleDataConverter.Packs(_syntax).Single().Sets.Single().Rows.Single();

    [Fact] void should_convert_grandchildren_to_an_array() => _result["lines"][0]["discounts"].ShouldBeOfExactType<JsonArray>();
    [Fact] void should_convert_grandchildren_to_objects() => _result["lines"][0]["discounts"].AsArray().All(child => child is JsonObject).ShouldBeTrue();
    [Fact] void should_keep_grandchild_values_and_order() => _result["lines"][0]["discounts"].AsArray().Select(child => child["amount"].GetValue<decimal>()).ShouldEqual([1.5m, 2.5m]);
    [Fact] void should_keep_the_child_sibling_field() => _result["lines"][0]["product"].GetValue<string>().ShouldEqual("Notebook");
}
