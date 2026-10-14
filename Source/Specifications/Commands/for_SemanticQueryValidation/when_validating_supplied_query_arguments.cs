// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Specifications.Commands.for_SemanticQueryValidation;

public class when_validating_supplied_query_arguments : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticKeyedQuery _query = null!;
    SemanticSpecificationQueryResult _request = null!;
    SemanticValidationFailure[] _failures = [];

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(QueryValidationCorpus.V18.EsmBytes.AsSpan())).Plan!;
        var keyed = _plan.Queries.Values.Single(query => query.Argument is not null);
        var filter = _plan.Queries.Values.Single(query => !query.Filters.IsEmpty).Filters.Single();
        var second = filter with { Id = keyed.Id, Name = "second", Type = keyed.Argument!.Type with { IsOptional = true } };
        var omitted = filter with { Id = keyed.Argument.Id, Name = "omitted" };
        _query = keyed with { Filters = [filter, second, omitted] };
        _request = new(keyed.Id, SemanticValue.Text("A"), [])
        {
            Filters = [new(second.Id, SemanticValue.Text("B")), new(filter.Id, SemanticValue.Text("C"))]
        };
    }

    void Because() => _failures = [.. SemanticQueryValidation.Failures(_plan, _query, _request)];

    [Fact] void should_validate_the_key_before_filters_in_declaration_order() => _failures.Select(failure => failure.Message).ShouldEqual(["Invoice number is too short", "Customer code is too short", "Invoice number is too short"]);
    [Fact] void should_not_validate_an_omitted_optional_filter() => _failures.Length.ShouldEqual(3);
}
