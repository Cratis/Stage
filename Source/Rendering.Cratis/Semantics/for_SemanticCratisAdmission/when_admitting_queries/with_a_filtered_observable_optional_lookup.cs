// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_a_filtered_observable_optional_lookup : given.a_query_shape
{
    void Establish()
    {
        Configure(SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Live, true);
        _context = Context();
        var property = _slice.ReadModels.Single().Properties.First(property => !property.IsIdentifier);
        _query = _query with { Filters = [new(SemanticId.Create(SemanticKind.Property, "query-filter"), property.Name, property.Type, property.Id)] };
        _slice = _slice with { Queries = [_query] };
    }

    void Because() => _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, [_context.Slice(_slice.Id) with { Slice = _slice }])];

    [Fact] void should_refuse_the_query_shape() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-010");
}
