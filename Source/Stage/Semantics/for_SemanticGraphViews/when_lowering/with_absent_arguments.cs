// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_absent_arguments : given.an_organization_with_views
{
    GraphQuery _query = null!;

    void Because() => _query = Lower("PeopleInGroup");

    [Fact] void should_keep_only_the_literal_condition() => _query.Clauses.Count.ShouldEqual(1);
    [Fact] void should_keep_it_in_the_related_clause() => ((GraphRelatedClause)_query.Clauses[0]).Clauses.ShouldEqual<GraphPropertyClause>(new GraphPropertyClause("name", "name", "Engineering"));
}
