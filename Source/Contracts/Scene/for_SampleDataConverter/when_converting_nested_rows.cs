// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Engine.SampleData;
using Cratis.Scene.Model.SampleData;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_converting_nested_rows : Specification
{
    IReadOnlyList<SampleDataPack> _packs;
    JsonObject _row;
    SampleDataResolution _resolved;

    void Establish()
    {
        var at = SourceLocation.Start;
        var lines = new SampleDataFieldSyntax("lines", SampleDataValueKind.Rows, null, at)
        {
            Rows =
            [
                new(
                    [
                        new("product", SampleDataValueKind.Text, "Notebook", at),
                        new("quantity", SampleDataValueKind.Number, "2", at),
                        new("price", SampleDataValueKind.Number, "4.5", at),
                        new("available", SampleDataValueKind.Flag, "true", at),
                        new("note", SampleDataValueKind.Null, null, at)
                    ],
                    at),
                new([new("product", SampleDataValueKind.Text, "Pencil", at)], at)
            ]
        };
        var row = new SampleDataRowSyntax(
            [
                new("id", SampleDataValueKind.Text, "order-1", at),
                lines,
                new("status", SampleDataValueKind.Text, "open", at)
            ],
            at);
        var syntax = new ApplicationSyntax([], [], [], [], at)
        {
            SampleDataPacks = [new("OrdersDemo", null, null, [new("Order", [row], at)], at)]
        };
        _packs = SampleDataConverter.Packs(syntax);
    }

    void Because()
    {
        _row = _packs.Single().Sets.Single().Rows.Single();
        var profile = UiProfileConverter.Convert(new UiProfileSyntax("Desktop", ["web"], null, ["core"], SourceLocation.Start)).Single() with
        {
            SampleData = new([], [], "orders-demo")
        };
        var request = new SampleDataRequest("Order", new("Order", [new("id", "String"), new("lines", "OrderLine[]"), new("status", "String")]));
        _resolved = SampleDataResolver.Resolve(request, new(_packs, profile, IsDesignOrPreview: true));
    }

    [Fact] void should_convert_children_to_an_array() => _row["lines"].ShouldBeOfExactType<JsonArray>();
    [Fact] void should_convert_children_to_objects() => _row["lines"].AsArray().All(child => child is JsonObject).ShouldBeTrue();
    [Fact] void should_keep_child_row_order() => _row["lines"].AsArray().Select(child => child["product"].GetValue<string>()).ShouldEqual(["Notebook", "Pencil"]);
    [Fact] void should_keep_the_sibling_fields_and_their_order() => _row.Select(field => field.Key).ShouldEqual(["id", "lines", "status"]);
    [Fact] void should_keep_sibling_values() => (_row["id"].GetValue<string>(), _row["status"].GetValue<string>()).ShouldEqual(("order-1", "open"));
    [Fact] void should_keep_a_child_whole_number() => _row["lines"][0]["quantity"].GetValue<long>().ShouldEqual(2L);
    [Fact] void should_keep_a_child_decimal() => _row["lines"][0]["price"].GetValue<decimal>().ShouldEqual(4.5m);
    [Fact] void should_keep_a_child_boolean() => _row["lines"][0]["available"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_keep_a_child_null() => _row["lines"][0]["note"].ShouldBeNull();
    [Fact] void should_resolve_the_active_pack_for_the_read_model() => _resolved.Source.ShouldEqual(SampleDataResolutionSource.ActivePack);
    [Fact] void should_pass_the_child_array_through_the_resolver() => JsonNode.DeepEquals(_resolved.Rows[0]["lines"], _row["lines"]).ShouldBeTrue();
    [Fact] void should_resolve_without_diagnostics() => _resolved.Diagnostics.ShouldBeEmpty();
}
