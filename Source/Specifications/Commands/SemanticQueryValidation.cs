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

        return arguments.Concat(supplied).SelectMany(argument => Validate(argument.Type, argument.Value, concepts, composites));
    }

    static IEnumerable<SemanticValidationFailure> Validate(
        SemanticTypeReference type,
        SemanticValue value,
        IReadOnlyDictionary<SemanticId, SemanticConcept> concepts,
        IReadOnlyDictionary<SemanticId, SemanticCompositeType> composites)
    {
        if (type.IsCollection)
        {
            return value is SemanticArrayValue array
                ? array.Values.SelectMany(element => Validate(type with { IsCollection = false, IsOptional = false }, element, concepts, composites))
                : [];
        }

        return type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => concepts[type.Target].Validations
                .Where(rule => !SemanticRuleEvaluation.Satisfies(rule, value))
                .Select(rule => new SemanticValidationFailure(rule.Message ?? SemanticRuleEvaluation.Message(rule, value, "A required concept value is empty."), rule.Severity)),
            SemanticTypeReferenceKind.CompositeType when value is SemanticCompositeValue composite => composite.Properties
                .SelectMany(property => Validate(composites[type.Target].Properties.Single(member => member.Id == property.TargetProperty).Type, property.Value, concepts, composites)),
            _ => []
        };
    }
}
