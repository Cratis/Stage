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
    static bool HasRenderableQueryExpectations(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenAbsentReadModels.IsEmpty && !DependsOnOpaquePolicy(context, specification) &&
        ProtectedCollectionRejectionReason(context, specification) is null;

    static bool CanRenderQueryOnly(SemanticApplicationContext context, SemanticSpecification specification) =>
        CanDenyQueryOnly(specification, context) ||
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
            if (context.Projections.Values.Any(projection => projection.ReadModel == query.ReadModel && projection.Scope is not null))
            {
                return "A protected live collection over a scoped projection needs default query-scenario observation of children read models (Chronicle#4816).";
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
