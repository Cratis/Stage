// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks the query scenario's supported cardinalities and delivery modes.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    static bool HasRenderableQueryExpectations(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenAbsentReadModels.IsEmpty && !DependsOnOpaquePolicy(context, specification);

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
            : null;
}
