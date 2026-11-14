// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_an_optional_collect_over_a_self_edge : given.an_organization_with_views
{
    GraphQueryRelation _relation = null!;

    void Because() => _relation = Lower("GroupsWithParent").Root.Members.Single(member => member.Name == "parent").Relation!;

    [Fact] void should_follow_the_self_edge_in_the_direction_of_the_arrow() => _relation.Direction.ShouldEqual(GraphViewDirection.Outgoing);
    [Fact] void should_hold_at_most_one_row() => _relation.Cardinality.ShouldEqual(GraphViewCardinality.One);
    [Fact] void should_name_the_nested_view_after_the_row_type() => _relation.Target.View.ShouldEqual("GroupSummary");
}
