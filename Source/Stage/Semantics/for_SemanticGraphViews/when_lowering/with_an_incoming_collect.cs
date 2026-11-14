// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_an_incoming_collect : given.an_organization_with_views
{
    GraphQueryRelation _relation = null!;

    void Because() => _relation = Lower("GroupsWithMembers").Root.Members.Single(member => member.Name == "members").Relation!;

    [Fact] void should_follow_the_edge_incoming() => _relation.Direction.ShouldEqual(GraphViewDirection.Incoming);
    [Fact] void should_reach_the_source_label() => _relation.Target.Label.ShouldEqual("Person");
    [Fact] void should_order_the_nested_rows_by_the_source_key() => _relation.Target.KeyProperty.ShouldEqual("personId");
}
