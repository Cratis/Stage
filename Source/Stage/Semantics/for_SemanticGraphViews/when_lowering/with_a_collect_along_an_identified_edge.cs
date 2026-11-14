// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_a_collect_along_an_identified_edge : given.an_organization_with_views
{
    GraphQueryRelation _relation = null!;

    void Because() => _relation = Lower("PeopleWithRoles").Root.Members.Single(member => member.Name == "roles").Relation!;

    [Fact] void should_order_the_nested_rows_by_the_identity_of_the_edge() => _relation.IdentifyingProperty.ShouldEqual("assignmentId");
    [Fact] void should_read_the_identity_and_the_role_from_the_edge() => _relation.Target.Members.Where(member => member.Kind == GraphViewMemberKind.EdgeProperty).Select(member => member.Source).ShouldEqual(["assignmentId", "role"]);
}
