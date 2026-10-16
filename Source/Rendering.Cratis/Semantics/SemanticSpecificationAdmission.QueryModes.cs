// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks the query scenario's supported cardinalities and delivery modes.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static bool CanEmptyQueryOnly(SemanticSpecification specification, SemanticApplicationContext context) =>
        specification.When is null && specification.WhenAppended is null && specification.WhenCapture is null &&
        specification.WhenTrigger is null && specification.WhenClock is null && specification.WhenRedelivered is null &&
        specification.GivenEvents.IsEmpty && specification.GivenReadModels.IsEmpty && specification.ThenEvents.IsEmpty &&
        specification.ThenReadModels.IsEmpty && specification.ThenQueries is [{ Results.IsEmpty: true } expected] &&
        specification.ThenErrors.Length <= 1 && !specification.ThenDenied &&
        specification.ThenErrors.All(error => error.Code is null && SemanticValidationRendering.CanResolve(error.Message, context)) &&
        context.Queries.TryGetValue(expected.Query, out var query) &&
        (query.Authorization is null || specification.GivenCaller is not null) &&
        !context.Reducers.Any(reducer => reducer.ReadModel == query.ReadModel) &&
        (specification.GivenCaller is null or { Authenticated: true } or { Roles.IsEmpty: true, Claims.IsEmpty: true }) &&
        specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) != true;

    static bool HasRenderableQueryExpectations(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenAbsentReadModels.IsEmpty && !DependsOnOpaquePolicy(context, specification) &&
        ProtectedCollectionRejectionReason(context, specification) is null && QueryArgumentsHaveCompleteValues(context, specification);

    static bool QueryArgumentsHaveCompleteValues(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenQueries.All(expected => !context.Queries.TryGetValue(expected.Query, out var query) ||
            ((query.Argument is null || HasCompleteValue(context, query.Argument.Type, expected.Key)) &&
                query.Filters.All(filter => expected.Filters.Where(value => value.Filter == filter.Id)
                    .All(value => HasCompleteValue(context, filter.Type, value.Value)))));

    static bool HasCompleteValue(SemanticApplicationContext context, SemanticTypeReference type, SemanticValue value)
    {
        if (type.IsCollection && value is SemanticArrayValue array)
        {
            return array.Values.All(element => HasCompleteValue(context, type with { IsCollection = false }, element));
        }
        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue composite)
        {
            return context.Types[type.Target].Properties.All(property =>
                composite.Properties.SingleOrDefault(member => member.TargetProperty == property.Id) is { } member &&
                HasCompleteValue(context, property.Type, member.Value));
        }

        return true;
    }

    static bool CanRenderQueryOnly(SemanticApplicationContext context, SemanticSpecification specification) =>
        CanEmptyQueryOnly(specification, context) || CanDenyQueryOnly(specification, context) ||
        (CanSeedQueryOnly(specification, context) && QueryMatches(context, specification.ThenQueries[0])) ||
        (CanProjectQueryOnly(specification, context) && QueryMatches(context, specification.ThenQueries[0]));

    static bool HasProtectedQueryWithRenderableKey(SemanticSpecificationQueryResult expected, SemanticApplicationContext context) =>
        context.Queries.TryGetValue(expected.Query, out var query) && query.Authorization is not null &&
        (query.Argument is null || IsScalar(expected.Key));

    static bool TryGetSeedableQuery(SemanticSpecificationQueryResult expected, SemanticApplicationContext context, out SemanticKeyedQuery query) =>
        context.Queries.TryGetValue(expected.Query, out query!) && query.Authorization is null &&
        query is { Cardinality: SemanticQueryCardinality.ZeroOrOne, Delivery: SemanticQueryDelivery.Snapshot };

    static string? AuthorizationRejectionReason(SemanticApplicationContext context, SemanticSpecification specification) =>
        DependsOnOpaquePolicy(context, specification)
            ? "The operation's authorization depends on an opaque csharp/file policy, which the reference evaluator cannot decide (Unsupported)."
            : ProtectedCollectionRejectionReason(context, specification);

    static string? ProtectedCollectionRejectionReason(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.ThenDenied) return null;
        foreach (var expected in specification.ThenQueries.Where(expected => !expected.Results.IsEmpty))
        {
            if (!context.Queries.TryGetValue(expected.Query, out var query) ||
                query.Authorization is null || query.Cardinality != SemanticQueryCardinality.Many)
            {
                continue;
            }
            if (query.Delivery != SemanticQueryDelivery.Live)
            {
                return "A protected snapshot collection success needs a collection-aware query scenario GetInstances store; Arc 22.87 forwards GetInstances to the inner store rather than its seeded events and pinned rows.";
            }
            var routedReplay = specification.GivenEvents.Any(given => given.Route is not null) ||
                SemanticSpecificationRouteRendering.ForCommand(specification, context) is not null;
            if (routedReplay && !HasReadablePinnedKey(context, query.ReadModel))
            {
                return "A protected live collection with routed replay pins rows that need a readable Key, Subject or Id; Arc 22.87 cannot observe unkeyed pinned read models.";
            }
        }

        return null;
    }

    static bool HasReadablePinnedKey(SemanticApplicationContext context, SemanticId readModel)
    {
        // Flat models emit [Key] only for the first single-result lookup's primitive/value key.
        // Event-source identifiers cannot carry [Key]; Arc also recognizes the generated Id member.
        var lookup = context.DeclaringSlice(readModel).Slice.Queries.FirstOrDefault(query =>
            query.ReadModel == readModel && query.Cardinality == SemanticQueryCardinality.ZeroOrOne);
        var types = new SemanticTypeSystem(context);

        return context.ReadModels[readModel].Properties.Any(property =>
            string.Equals(Identifiers.ToPascalCase(property.Name), "Id", StringComparison.Ordinal) ||
            (lookup?.KeyProperty == property.Id && !types.IsEventSourceIdentifier(property.Type)));
    }
}
