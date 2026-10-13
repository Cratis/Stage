// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_a_colliding_filter_name : given.a_query_shape
{
    void Establish()
    {
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, false);
        var property = _slice.ReadModels.Single().Properties.Single(property => property.Name == "name");
        _query = _query with { Filters = [new(SemanticId.Create(SemanticKind.Property, "query-filter"), "read_models", property.Type, property.Id)] };
        var model = _slice.ReadModels.Single();
        _slice = _slice with
        {
            Queries = [_query],
            ReadModels = [model with { Properties = [.. model.Properties.Select(value => value.Id == property.Id ? value with { Name = "read_models" } : value)] }]
        };
    }

    void Because() => Evaluate();

    [Fact] void should_refuse_the_generated_parameter_collision() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-012");
}
