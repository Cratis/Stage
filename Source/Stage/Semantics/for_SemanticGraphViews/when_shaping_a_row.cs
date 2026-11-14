// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews;

public class when_shaping_a_row : Specification
{
    IReadOnlyDictionary<string, JsonElement> _values = null!;

    void Because() => _values = SemanticGraphViews.Values(new GraphRow(new Dictionary<string, object?>
    {
        ["id"] = "p1",
        ["name"] = null,
        ["groups"] = new List<GraphRow> { new(new Dictionary<string, object?> { ["id"] = "g1", ["since"] = "2026-01-01T00:00:00Z" }) },
        ["roles"] = new List<GraphRow>(),
        ["parent"] = null,
        ["manager"] = new GraphRow(new Dictionary<string, object?> { ["id"] = "p2" })
    }));

    [Fact] void should_keep_a_scalar() => _values["id"].GetString().ShouldEqual("p1");
    [Fact] void should_keep_an_absent_property_as_null() => _values["name"].ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_shape_a_relation_of_many_as_an_array_of_objects() => _values["groups"].GetRawText().ShouldEqual("""[{"id":"g1","since":"2026-01-01T00:00:00Z"}]""");
    [Fact] void should_shape_a_relation_of_many_with_nothing_related_as_an_empty_array() => _values["roles"].GetRawText().ShouldEqual("[]");
    [Fact] void should_shape_a_relation_of_one_with_nothing_related_as_null() => _values["parent"].ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_shape_a_related_row_as_an_object() => _values["manager"].GetRawText().ShouldEqual("""{"id":"p2"}""");
}
