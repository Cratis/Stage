// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits semantic specifications the generated Cratis scenario family can preserve.
/// </summary>
internal static class SemanticSpecificationAdmission
{
    /// <summary>
    /// Validates the specifications declared by one slice.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slice">The declaring slice.</param>
    /// <param name="diagnostics">The diagnostics to append to.</param>
    public static void Validate(
        SemanticApplicationContext context,
        SemanticSlice slice,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications)
        {
            var valid = context.Commands.TryGetValue(specification.When.Command, out var command) &&
                specification.GivenEvents.IsEmpty && specification.GivenReadModels.IsEmpty &&
                ValuesMatch(specification.When.Values, command?.Properties ?? []) &&
                HasOneOutcome(specification) && HasSupportedCounts(specification) &&
                specification.ThenEvents.All(expected => EventMatches(context, expected) &&
                    command!.Produces.Any(produced => produced.EventContract == expected.EventContract)) &&
                specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
                    HasExpectedProjectionEvent(context, specification, expected)) &&
                specification.ThenQueries.All(expected => QueryMatches(context, expected)) &&
                specification.ThenErrors.All(_ => _.Code is null);

            if (!valid)
            {
                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' exceeds the first generated Cratis specification capability.",
                    specification.Id));
            }
        }
    }

    static bool HasOneOutcome(SemanticSpecification specification)
    {
        var rejects = !specification.ThenErrors.IsEmpty;
        var succeeds = !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty;
        return rejects != succeeds;
    }

    static bool HasSupportedCounts(SemanticSpecification specification) =>
        specification.ThenEvents.Length <= 1 && specification.ThenReadModels.Length <= 1 &&
        specification.ThenQueries.Length <= 1 && specification.ThenErrors.Length <= 1;

    static bool HasExpectedProjectionEvent(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected)
    {
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        return projection?.Transitions.Length == 1 &&
            specification.ThenEvents.Any(_ => _.EventContract == projection.Transitions[0].EventContract);
    }

    static bool EventMatches(SemanticApplicationContext context, SemanticSpecificationEvent expected) =>
        context.Events.TryGetValue(expected.EventContract, out var @event) && ValuesMatch(expected.Values, @event.Properties);

    static bool ReadModelMatches(SemanticApplicationContext context, SemanticSpecificationReadModel expected) =>
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        ValuesMatch(expected.Values, readModel.Properties) && IsScalar(expected.Key);

    static bool QueryMatches(SemanticApplicationContext context, SemanticSpecificationQueryResult expected) =>
        context.Queries.TryGetValue(expected.Query, out var query) && IsScalar(expected.Key) && expected.Results.Length == 1 &&
        expected.Results.All(result => result.ReadModel == query.ReadModel && ReadModelMatches(context, result));

    static bool ValuesMatch(
        System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values,
        System.Collections.Immutable.ImmutableArray<SemanticProperty> properties) =>
        values.Length == properties.Length && properties.All(property =>
            values.Any(value => value.TargetProperty == property.Id && IsCompatible(value.Value, property.Type)));

    static bool IsCompatible(SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue)
        {
            return type.IsOptional;
        }

        if (type.IsCollection)
        {
            return value is SemanticArrayValue array && array.Values.All(IsScalar);
        }

        return IsScalar(value);
    }

    static bool IsScalar(SemanticValue value) => value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;
}
