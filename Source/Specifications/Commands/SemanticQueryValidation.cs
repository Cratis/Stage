// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticQueryValidation
{
    internal static IEnumerable<SemanticValidationFailure> Failures(SemanticExecutionPlan plan, SemanticKeyedQuery query, SemanticSpecificationQueryResult request)
    {
        var concepts = plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
        var composites = plan.Model.Application.Types.ToDictionary(composite => composite.Id);
        var arguments = query.Argument is { } key ? new[] { (key.Type, Value: request.Key) } : [];
        var supplied = query.Filters.Where(filter => request.Filters.Any(value => value.Filter == filter.Id))
            .Select(filter => (filter.Type, request.Filters.Single(value => value.Filter == filter.Id).Value));

        return arguments.Concat(supplied).SelectMany(argument => SemanticConceptValues.Validate(argument.Type, argument.Value, concepts, composites));
    }
}
