// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_a_filtered_snapshot_list : given.a_query_shape
{
    void Establish()
    {
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Snapshot, false);
        var property = _slice.ReadModels.Single().Properties.Single(property => property.Name == "name");
        _query = _query with { Filters = [new(SemanticId.Create(SemanticKind.Property, "query-filter"), "name", property.Type, property.Id)] };
        _slice = _slice with { Queries = [_query] };
    }

    void Because() => Evaluate();

    [Fact] void should_admit_the_snapshot_collection_shape() => _diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
}
