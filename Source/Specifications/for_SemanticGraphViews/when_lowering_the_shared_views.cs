// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticGraphViews;

/// <summary>
/// Lowers every query of the fixture model and holds each view it reads, the root and every nested view, to the shared declaration of
/// the Organization views (graph-views-organization.json): the same name, root label and members in declaration order, each relation
/// with its edge, direction, cardinality and target view. A collect maps one-to-one onto a relation of a view.
/// </summary>
public class when_lowering_the_shared_views : given.the_shared_graph_views
{
    readonly Dictionary<string, JsonNode> _lowered = [];
    JsonArray _declared = null!;

    void Because()
    {
        _declared = Fixture("graph-views-organization.json")["views"]!.AsArray();
        foreach (var query in _application.GraphQueries)
        {
            Collect(SemanticGraphViews.Lower(_application, query, ReadModelOf(query), new Dictionary<string, object?>()).Root);
        }
    }

    [Fact] void should_read_every_declared_view() => _lowered.Keys.Order(StringComparer.Ordinal).ShouldEqual(_declared.Select(view => view!["name"]!.GetValue<string>()));

    [Fact]
    void should_lower_each_view_exactly_as_declared() =>
        _declared.Where(view => !JsonNode.DeepEquals(view, _lowered.GetValueOrDefault(view!["name"]!.GetValue<string>()))).Select(view => view!["name"]!.GetValue<string>()).ShouldBeEmpty();

    void Collect(GraphQueryNode node)
    {
        _lowered[node.View] = new JsonObject
        {
            ["name"] = node.View,
            ["root"] = node.Label,
            ["members"] = new JsonArray([.. node.Members.Select(Member)])
        };
        foreach (var relation in node.Members.Select(member => member.Relation).OfType<GraphQueryRelation>()) Collect(relation.Target);
    }

    static JsonNode Member(GraphQueryMember member) => member.Relation is { } relation
        ? new JsonObject
        {
            ["name"] = member.Name,
            ["kind"] = "relation",
            ["edge"] = relation.Edge,
            ["direction"] = relation.Direction == GraphViewDirection.Outgoing ? "outgoing" : "incoming",
            ["cardinality"] = relation.Cardinality == GraphViewCardinality.Many ? "many" : "one",
            ["view"] = relation.Target.View
        }
        : new JsonObject
        {
            ["name"] = member.Name,
            ["kind"] = member.Kind == GraphViewMemberKind.EdgeProperty ? "edgeProperty" : "property",
            ["source"] = member.Source
        };
}
