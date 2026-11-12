// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    // The first matching rule explains the refusal; the order of the rules is part of the reported behavior.
    static string RejectionReason(SemanticApplicationContext context, SemanticSpecification specification) =>
        RejectQueryShape(context, specification) ??
        RejectExpectations(context, specification) ??
        RejectProtectedQueryScenario(context, specification) ??
        RejectFixtures(context, specification) ??
        RejectIncomparableResponse(context, specification) ??
        "The scenario cannot preserve all of its declared fixtures or expectations.";

    static string? RejectQueryShape(SemanticApplicationContext context, SemanticSpecification specification) =>
        RejectSeededQueryErrors(specification) ??
        RejectSparseQueryArguments(context, specification) ??
        AuthorizationRejectionReason(context, specification) ??
        RejectUncontrolledOccurrence(context, specification);

    static string? RejectExpectations(SemanticApplicationContext context, SemanticSpecification specification) =>
        RejectUnassertableAbsence(context, specification) ??
        RejectUnorderedDuplicates(context, specification) ??
        RejectReducerQueries(context, specification) ??
        RejectReducerReadModels(context, specification) ??
        RejectScopedPresence(context, specification);

    static string? RejectProtectedQueryScenario(SemanticApplicationContext context, SemanticSpecification specification) =>
        RejectDeniedQueryWithoutCommand(specification) ??
        RejectProtectedQueries(context, specification) ??
        RejectProtectedQuerySources(context, specification);

    static string? RejectFixtures(SemanticApplicationContext context, SemanticSpecification specification) =>
        RejectGivenReadModels(specification) ??
        RejectSourcelessGivenEvents(context, specification) ??
        RejectConstraintViolatingGivenEvents(context, specification) ??
        RejectRoleClaimCaller(specification);

    static string? RejectSeededQueryErrors(SemanticSpecification specification) =>
        specification.When is null && !specification.ThenQueries.IsEmpty && !specification.ThenErrors.IsEmpty && HasGivenFixtures(specification)
            ? "Query validation rejection specifications with given events or read-model state require a seeded Arc query scenario; only empty fixtures are supported."
            : null;

    static bool HasGivenFixtures(SemanticSpecification specification) => !specification.GivenEvents.IsEmpty || !specification.GivenReadModels.IsEmpty;

    static string? RejectSparseQueryArguments(SemanticApplicationContext context, SemanticSpecification specification) =>
        QueryArgumentsHaveCompleteValues(context, specification)
            ? null
            : "Sparse composite query arguments cannot render: every member, including optional members, must be supplied explicitly to preserve concept validation without inventing CLR defaults.";

    static string? RejectUncontrolledOccurrence(SemanticApplicationContext context, SemanticSpecification specification) =>
        InvokedCommand(context, specification) is { } command && AssertsUncontrolledOccurrence(context.Application, specification, command)
            ? "A command maps $context.occurred from its current clock; fixed event or projected values in a specification cannot assert this occurrence without a supplied time."
            : null;

    static SemanticCommand? InvokedCommand(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.When is { } when && context.Commands.TryGetValue(when.Command, out var command) ? command : null;

    static string? RejectUnassertableAbsence(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenAbsentReadModels.Any(expected => !CanAssertAbsence(context, specification, expected)) ? AbsenceRejection : null;

    static string? RejectUnorderedDuplicates(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenEventsInAnyOrder &&
        specification.ThenEvents.GroupBy(_ => _.EventContract).Any(group => group.Count() > 1) &&
        specification.ThenReadModels.Any(expected => HasReducer(context, expected.ReadModel))
            ? "Unordered duplicate event contracts cannot be paired with reducer replay occurrences."
            : null;

    static bool HasReducer(SemanticApplicationContext context, SemanticId readModel) => context.Reducers.Any(reducer => reducer.ReadModel == readModel);

    static string? RejectReducerQueries(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenQueries.Any(expected => context.Queries.TryGetValue(expected.Query, out var query) && HasReducer(context, query.ReadModel))
            ? "Query expectations on reducer-built read models require the rendered target's query scenario, which is not admitted in Phase 1."
            : null;

    static string? RejectReducerReadModels(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenReadModels.Select(expected => ReducerRejectionReason(context, expected.ReadModel)).FirstOrDefault(reason => reason is not null);

    static string? RejectScopedPresence(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenReadModels.Any(_ => CannotCompareScopedPresence(context, _.ReadModel, _.Exactly, _.Values)) ||
        specification.ThenQueries.Any(expected => CannotCompareScopedQueryPresence(context, expected))
            ? "An exactly or null comparison of a scoped projection must distinguish unset properties from CLR defaults; the generated ReadModelScenario exposes only the materialized record."
            : null;

    static bool CannotCompareScopedQueryPresence(SemanticApplicationContext context, SemanticSpecificationQueryResult expected) =>
        context.Queries.TryGetValue(expected.Query, out var query) &&
        (CannotCompareScopedPresence(context, query.ReadModel, expected.Exactly, []) ||
            expected.Results.Any(result => CannotCompareScopedPresence(context, query.ReadModel, result.Exactly, result.Values)));

    static string? RejectDeniedQueryWithoutCommand(SemanticSpecification specification) =>
        specification.ThenDenied && specification.When is null
            ? "A denied query must be executed through Arc's query pipeline to assert Unauthorized and no returned data; a direct read-model lookup does not evaluate its policy."
            : null;

    static string? RejectProtectedQueries(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenQueries.Any(expected => context.Queries.TryGetValue(expected.Query, out var query) && IsProtected(context, query))
            ? "A protected query must run through Arc's query pipeline with the fixture caller; direct invocation bypasses the generated authorization policy."
            : null;

    static string? RejectProtectedQuerySources(SemanticApplicationContext context, SemanticSpecification specification) =>
        InvokedCommand(context, specification) is { } command && !ProtectedQuerySourcesMatch(context, specification, command)
            ? "A protected query scenario replays only events from its query key; every seeded and produced event must use that source."
            : null;

    static string? RejectGivenReadModels(SemanticSpecification specification) =>
        specification.GivenReadModels.IsEmpty
            ? null
            : "Given read-model state requires complete non-null scalar records with identifiers matching their keys and independent projections. Seeded command reads, reducers and query combinations are not supported; a query-only lookup of one complete seeded record is supported.";

    static string? RejectSourcelessGivenEvents(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.GivenEvents.Any(fact => MissingObservableSource(context, fact)) ? SourcelessGivenReason : null;

    static string? RejectConstraintViolatingGivenEvents(SemanticApplicationContext context, SemanticSpecification specification) =>
        GivenEventsViolateConstraints(context, specification)
            ? "Given events violate a selected append-time constraint and cannot be seeded faithfully into Chronicle's event log."
            : null;

    static string? RejectRoleClaimCaller(SemanticSpecification specification) =>
        specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) == true
            ? "A caller claim using the role claim URI conflates separate Screenplay roles and claims in Arc."
            : null;

    static string? RejectIncomparableResponse(SemanticApplicationContext context, SemanticSpecification specification) =>
        InvokedCommand(context, specification) is { } command && !HasComparableResponse(context, specification, command)
            ? CompositeResponseRejection
            : null;
}
