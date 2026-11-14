// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_an_outgoing_collect_and_conditions : given.an_organization_with_views
{
    GraphQuery _query = null!;
    GraphQueryRelation _relation = null!;

    void Because()
    {
        _query = Lower("PeopleInGroup", ("groupId", "g1"), ("name", "Ada"));
        _relation = _query.Root.Members.Single(member => member.Kind == GraphViewMemberKind.Relation).Relation!;
    }

    [Fact] void should_read_the_declared_graph() => _query.Graph.ShouldEqual("Organization");
    [Fact] void should_return_every_root() => _query.Shape.ShouldEqual(GraphQueryShape.List);
    [Fact] void should_name_the_view_after_the_read_model() => _query.Root.View.ShouldEqual("PersonWithGroups");
    [Fact] void should_anchor_the_view_at_the_root_label() => _query.Root.Label.ShouldEqual("Person");
    [Fact] void should_order_by_the_key_of_the_root() => _query.Root.KeyProperty.ShouldEqual("personId");
    [Fact] void should_keep_the_read_model_property_order() => _query.Root.Members.Select(member => member.Name).ShouldEqual(["id", "name", "groups"]);
    [Fact] void should_read_the_scalars_from_the_root() => _query.Root.Members.Take(2).Select(member => (member.Kind, member.Source)).ShouldEqual([(GraphViewMemberKind.Property, "personId"), (GraphViewMemberKind.Property, "name")]);
    [Fact] void should_follow_the_edge_type() => _relation.Edge.ShouldEqual("MEMBER_OF");
    [Fact] void should_follow_it_outgoing() => _relation.Direction.ShouldEqual(GraphViewDirection.Outgoing);
    [Fact] void should_take_the_cardinality_of_the_collect() => _relation.Cardinality.ShouldEqual(GraphViewCardinality.Many);
    [Fact] void should_have_no_identifying_property_for_an_edge_without_identity() => _relation.IdentifyingProperty.ShouldBeNull();
    [Fact] void should_name_the_nested_view_after_the_row_type() => _relation.Target.View.ShouldEqual("GroupMembership");
    [Fact] void should_anchor_the_nested_view_at_the_reached_label() => _relation.Target.Label.ShouldEqual("Group");
    [Fact] void should_order_the_nested_rows_by_the_reached_key() => _relation.Target.KeyProperty.ShouldEqual("groupId");

    [Fact]
    void should_read_the_row_from_the_reached_node_and_the_followed_edge() =>
        _relation.Target.Members.Select(member => (member.Name, member.Kind, member.Source)).ShouldEqual(
            [("id", GraphViewMemberKind.Property, "groupId"), ("name", GraphViewMemberKind.Property, "name"), ("since", GraphViewMemberKind.EdgeProperty, "since")]);

    [Fact] void should_select_roots_by_a_related_clause_on_the_collect() => ((GraphRelatedClause)_query.Clauses[0]).Member.ShouldEqual("groups");

    [Fact]
    void should_hold_every_condition_on_the_collected_node_in_the_one_related_clause() =>
        ((GraphRelatedClause)_query.Clauses[0]).Clauses.ShouldEqual<GraphPropertyClause>(new GraphPropertyClause("id", "groupId", "g1"), new GraphPropertyClause("name", "name", "Engineering"));

    [Fact] void should_select_roots_by_a_property_clause_on_the_root() => _query.Clauses[1].ShouldEqual(new GraphPropertyClause("name", "name", "Ada"));
    [Fact] void should_have_one_clause_per_selected_element() => _query.Clauses.Count.ShouldEqual(2);
}
