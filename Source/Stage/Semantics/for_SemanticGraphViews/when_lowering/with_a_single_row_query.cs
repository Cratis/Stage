// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_a_single_row_query : given.an_organization_with_views
{
    GraphQuery _query = null!;

    void Because() => _query = Lower("PersonById", ("personId", "p1"));

    [Fact] void should_ask_for_the_first_root() => _query.Shape.ShouldEqual(GraphQueryShape.First);
    [Fact] void should_select_the_root_by_its_key() => _query.Clauses.ShouldEqual<GraphQueryClause>(new GraphPropertyClause("id", "personId", "p1"));
}
