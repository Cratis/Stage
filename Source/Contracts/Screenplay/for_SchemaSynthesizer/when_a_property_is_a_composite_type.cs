// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_SchemaSynthesizer;

public class when_a_property_is_a_composite_type : Specification
{
    static PropertySyntax Property(string name, string type, bool collection = false, bool optional = false) =>
        new(name, new TypeRefSyntax(type, collection, optional, SourceLocation.Start), SourceLocation.Start);

    JsonObject _schema = null!;

    void Because()
    {
        var concepts = new Dictionary<string, ConceptSyntax> { ["Country"] = new("Country", "Enum", [], ["NO", "SE"], SourceLocation.Start) };
        var types = new Dictionary<string, TypeSyntax>
        {
            ["PostalAddress"] = new("PostalAddress", [Property("street", "String"), Property("country", "Country"), Property("note", "String", optional: true)], SourceLocation.Start),
            ["OrderLine"] = new("OrderLine", [Property("sku", "String"), Property("quantity", "Int")], SourceLocation.Start),
        };
        _schema = JsonNode.Parse(new SchemaSynthesizer(concepts, types).ForProperties([Property("shipTo", "PostalAddress"), Property("lines", "OrderLine", collection: true), Property("blob", "Unknown")]))!.AsObject();
    }

    [Fact] void should_carry_the_members_of_the_composite() => _schema["properties"]!["shipTo"]!["properties"]!.AsObject().Select(member => member.Key).ShouldEqual(["street", "country", "note"]);
    [Fact] void should_resolve_a_member_typed_as_an_enumeration() => _schema["properties"]!["shipTo"]!["properties"]!["country"]!["enum"]!.AsArray().Select(value => value!.GetValue<string>()).ShouldEqual(["NO", "SE"]);
    [Fact] void should_require_only_the_members_that_are_not_optional() => _schema["properties"]!["shipTo"]!["required"]!.AsArray().Select(value => value!.GetValue<string>()).ShouldEqual(["street", "country"]);
    [Fact] void should_carry_the_members_of_a_collection_item() => _schema["properties"]!["lines"]!["items"]!["properties"]!.AsObject().Select(member => member.Key).ShouldEqual(["sku", "quantity"]);
    [Fact] void should_leave_an_undeclared_type_open() => _schema["properties"]!["blob"]!["properties"].ShouldBeNull();
}
