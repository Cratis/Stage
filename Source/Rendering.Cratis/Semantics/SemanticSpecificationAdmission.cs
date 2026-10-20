// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits semantic specifications the generated Cratis scenario family can preserve.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// The code that refuses a specification whose given or appended event has no event source.
    /// </summary>
    internal const string SourcelessGivenCode = "STAGE-ESM-051";
    const string SourcelessGivenReason = "A sourceless given fact has a null destination in the reference world; Chronicle scenario events require an event source id.";

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
        foreach (var specification in WithoutReactionCascades(context, slice, diagnostics))
        {
            if (SemanticProjectionFactSpecification.TryLower(context, specification, out var facts))
            {
                if (!CanRenderProjectedFacts(context, facts))
                {
                    diagnostics.Add(RefusalDiagnostic(context, specification, ProjectedFactsRejection(context, facts)));
                }

                continue;
            }
            if (AppendRefusedConstraint(context, specification) is { } refusing)
            {
                if (AppendRefusalRejection(context, specification, refusing) is { } refusal)
                {
                    diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {refusal}", specification.Id));
                }

                continue;
            }
            if (specification.WhenAppended is not null || specification.WhenRedelivered is not null)
            {
                ValidateReactionAppend(context, specification, diagnostics);
                continue;
            }
            var valid = CanRenderSpecification(context, specification);

            if (!valid)
            {
                var reason = RejectionReason(context, specification);
                diagnostics.Add(RefusalDiagnostic(context, specification, reason));
            }
        }
    }

    internal static IEnumerable<SemanticReactionTrigger> EventReactionTriggers(SemanticApplicationContext context) =>
        SemanticVersionFeatures.Slices(context.Application).SelectMany(slice => slice.Reactions.IsDefault ? [] : slice.Reactions)
            .SelectMany(reaction => reaction.Triggers).Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event);

    internal static bool CommandTriggersReaction(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action) return false;
        if (!context.Commands.TryGetValue(action.Command, out var command)) return false;

        return EventReactionTriggers(context).Any(trigger => SemanticProductionSelection.Taken(context.Application, command, specification).Any(produced => produced.EventContract == trigger.Source));
    }

    internal static bool IsReactionConstraintTarget(SemanticApplicationContext context, SemanticId @event) =>
        context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == @event) || selected.Constraint.ReleasedBy.Contains(@event));

    internal static bool TriggerValuesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        CommandValuesMatch(context, specification, command);

    static bool CanRenderSpecification(SemanticApplicationContext context, SemanticSpecification specification, bool reactionInvocation = false) =>
        HasRenderableQueryExpectations(context, specification) && (CanRenderQueryOnly(context, specification) ||
                (HasRenderableCallerAndCommand(context, specification, out var command, reactionInvocation) &&
                    HasRenderableGivenEvents(context, specification) && GivenKeysMatchProjectedProperties(context, specification) &&
                    !AssertsUncontrolledOccurrence(context.Application, specification, command!) &&
                    !GivenEventsViolateConstraints(context, specification) && CanSeedProjectedAbsence(context, specification) &&
                    CommandValuesMatch(context, specification, command!) &&
                    HasOneOutcome(specification) && HasSupportedCounts(specification) &&
                    (specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty || specification.ThenEvents.Length == SemanticProductionSelection.Taken(context.Application, command!, specification).Length) &&
                    specification.ThenEvents.All(expected => EventMatches(context, expected)) &&
                    (specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty ||
                        (specification.ThenEventsInAnyOrder
                            ? specification.ThenEvents.GroupBy(_ => _.EventContract).All(group =>
                                SemanticProductionSelection.Taken(context.Application, command!, specification).Count(_ => _.EventContract == group.Key) == group.Count())
                            : specification.ThenEvents.Select(_ => _.EventContract).SequenceEqual(SemanticProductionSelection.Taken(context.Application, command!, specification).Select(_ => _.EventContract)))) &&
                    specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
                        HasExpectedProjectionEvent(context, specification, expected) &&
                        !ReducerUsesUnstableContext(context, expected.ReadModel)) &&
                    specification.ThenAbsentReadModels.All(expected => CanAssertAbsence(context, specification, expected)) &&
                    specification.ThenQueries.All(expected => QueryMatches(context, expected) &&
                        !context.Reducers.Any(reducer => reducer.ReadModel == context.Queries[expected.Query].ReadModel) &&
                        expected.Results.All(result => HasExpectedProjectionEvent(context, specification, result))) &&
                    HasRenderableErrors(context, specification, command) &&
                    HasRenderableEventSources(context, specification, command!) &&
                    ProtectedQuerySourcesMatch(context, specification, command!) &&
                    HasComparableResponse(context, specification, command!)));

    static IEnumerable<SemanticSpecification> WithoutReactionCascades(SemanticApplicationContext context, SemanticSlice slice, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in WithRenderableGeneratedValues(context, slice.Specifications.Select(specification => specification.WithPinnedActionValues()), diagnostics))
        {
            if (CommandTriggersReaction(context, specification))
            {
                if (SemanticReactionCascade.IsRejectedByTrigger(context, specification))
                {
                    yield return specification;
                    continue;
                }

                if (SemanticReactionCascade.TryLower(context, specification, out var cascade))
                {
                    ValidateCascade(context, cascade, diagnostics);
                    continue;
                }

                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' cannot render: the command produces an event that triggers a reaction; generated CommandScenario specifications do not settle reactions yet.",
                    specification.Id));
                continue;
            }
            yield return specification;
        }
    }

    static void ValidateCascade(SemanticApplicationContext context, SemanticReactionCascade cascade, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        ValidateReactionAppend(context, cascade.Reaction, diagnostics);
        if (cascade.Trigger is { } trigger && !CanRenderSpecification(context, trigger))
        {
            diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{cascade.Reaction.Name}' cannot render: its triggering command specification cannot be preserved ({RejectionReason(context, trigger)}).", cascade.Reaction.Id));
        }
    }

    static bool ProtectedQuerySourcesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command)
    {
        foreach (var expected in specification.ThenQueries.Where(result =>
            context.Queries.TryGetValue(result.Query, out var query) && query.Authorization is not null))
        {
            // Keyed lookups still read only their selected stream; collection scenarios preserve all seeded rows.
            if (context.Queries[expected.Query].Cardinality == SemanticQueryCardinality.Many) continue;
            if (specification.GivenEvents.Any(given => !Equals(given.EventSource?.Value, expected.Key)) ||
                SemanticProductionSelection.Taken(context.Application, command, specification).Any(produced => !Equals(SemanticDestinations.ForSpecification(specification, command, produced).Value, expected.Key)))
            {
                return false;
            }
        }

        return true;
    }

    // The reference evaluator returns Unsupported once an opaque policy is reached, so it cannot be the oracle
    // for a generated scenario of an operation whose authorization depends on one.
    static bool DependsOnOpaquePolicy(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var authorizations = specification.ThenQueries.Select(expected => context.Queries.TryGetValue(expected.Query, out var query) ? query.Authorization : null);
        if (specification.When is { } action && context.Commands.TryGetValue(action.Command, out var command))
        {
            authorizations = authorizations.Append(command.Authorization);
        }

        return authorizations.OfType<SemanticAuthorization>()
            .Any(authorization => SemanticCratisAdmission.OpaquePolicies(authorization, context.Application.Policies).Any());
    }

    static bool AssertsUncontrolledOccurrence(SemanticApplication application, SemanticSpecification specification, SemanticCommand command) =>
        !SemanticSpecificationClock.TryGet(specification, out _) && SemanticProductionSelection.Taken(application, command, specification).Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred })) &&
        (!specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty || !specification.ThenQueries.IsEmpty);

    static bool ReducerUsesUnstableContext(SemanticApplicationContext context, SemanticId readModel)
    {
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == readModel);
        if (reducer is null) return false;
        foreach (var transition in reducer.Transitions)
        {
            var descriptor = context.Request.TypedContextDescriptors.SingleOrDefault(_ => _.RequirementId == transition.RequirementId && _.OperationId == readModel);
            var requirement = context.Request.ImplementationRequirements.SingleOrDefault(_ => _.RequirementId == transition.RequirementId);
            if (descriptor is null || requirement is null) return true;
            var diagnostics = System.Collections.Immutable.ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
            if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, transition.RequirementId, readModel, diagnostics, out var body)) return true;

            // Specification admission must fail closed too: a host without the audited
            // reference assemblies cannot safely classify this reducer's context reads.
            try
            {
                var verdict = PureTransitionAdmission.Analyze(body!, context, context.ReadModels[readModel], context.Events[transition.EventContract], descriptor, requirement);
                if (!verdict.Accepted || verdict.ContextReads.Overlaps(["Occurred", "SequenceNumber", "Tenant"])) return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        return false;
    }

    static string RejectionReason(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is null && !specification.ThenQueries.IsEmpty && !specification.ThenErrors.IsEmpty &&
            (!specification.GivenEvents.IsEmpty || !specification.GivenReadModels.IsEmpty))
        {
            return "Query validation rejection specifications with given events or read-model state require a seeded Arc query scenario; only empty fixtures are supported.";
        }

        if (!QueryArgumentsHaveCompleteValues(context, specification))
        {
            return "Sparse composite query arguments cannot render: every member, including optional members, must be supplied explicitly to preserve concept validation without inventing CLR defaults.";
        }

        if (AuthorizationRejectionReason(context, specification) is { } authorizationReason)
        {
            return authorizationReason;
        }

        if (specification.When is { } when && context.Commands.TryGetValue(when.Command, out var command) &&
            AssertsUncontrolledOccurrence(context.Application, specification, command))
        {
            return "A command maps $context.occurred from its current clock; fixed event or projected values in a specification cannot assert this occurrence without a supplied time.";
        }

        if (specification.ThenAbsentReadModels.Any(expected => !CanAssertAbsence(context, specification, expected)))
        {
            return AbsenceRejection;
        }

        if (specification.ThenEventsInAnyOrder &&
            specification.ThenEvents.GroupBy(_ => _.EventContract).Any(group => group.Count() > 1) &&
            specification.ThenReadModels.Any(expected => context.Reducers.Any(reducer => reducer.ReadModel == expected.ReadModel)))
        {
            return "Unordered duplicate event contracts cannot be paired with reducer replay occurrences.";
        }

        if (specification.ThenQueries.Any(expected => context.Queries.TryGetValue(expected.Query, out var query) &&
            context.Reducers.Any(reducer => reducer.ReadModel == query.ReadModel)))
        {
            return "Query expectations on reducer-built read models require the rendered target's query scenario, which is not admitted in Phase 1.";
        }

        if (specification.ThenReadModels.Any(expected => ReducerUsesUnstableContext(context, expected.ReadModel)))
        {
            return "A reducer body reads context.Occurred, context.SequenceNumber or context.Tenant, which Chronicle's ReadModelScenario cannot supply deterministically.";
        }

        if (specification.ThenReadModels.Any(_ => CannotCompareScopedPresence(context, _.ReadModel, _.Exactly, _.Values)) ||
            specification.ThenQueries.Any(_ => context.Queries.TryGetValue(_.Query, out var query) &&
                (CannotCompareScopedPresence(context, query.ReadModel, _.Exactly, []) ||
                    _.Results.Any(result => CannotCompareScopedPresence(context, query.ReadModel, result.Exactly, result.Values)))))
        {
            return "An exactly or null comparison of a scoped projection must distinguish unset properties from CLR defaults; the generated ReadModelScenario exposes only the materialized record.";
        }

        if (specification.ThenDenied && specification.When is null)
        {
            return "A denied query must be executed through Arc's query pipeline to assert Unauthorized and no returned data; a direct read-model lookup does not evaluate its policy.";
        }

        if (specification.ThenQueries.Any(_ => context.Queries.TryGetValue(_.Query, out var query) && query.Authorization is not null))
        {
            return "A protected query must run through Arc's query pipeline with the fixture caller; direct invocation bypasses the generated authorization policy.";
        }

        if (specification.When is { } action && context.Commands.TryGetValue(action.Command, out var producingCommand) &&
            !ProtectedQuerySourcesMatch(context, specification, producingCommand))
        {
            return "A protected query scenario replays only events from its query key; every seeded and produced event must use that source.";
        }

        if (!specification.GivenReadModels.IsEmpty)
        {
            return "Given read-model state replaces projected state before the action in the reference world. Chronicle's scenario seed serves lookup interception but cannot initialize each projected instance for subsequent command events; only a query-only lookup of one complete seeded record is supported.";
        }

        if (specification.GivenEvents.Any(_ => _.EventSource is null))
        {
            return SourcelessGivenReason;
        }

        if (GivenEventsViolateConstraints(context, specification))
        {
            return "Given events violate a selected append-time constraint and cannot be seeded faithfully into Chronicle's event log.";
        }

        if (specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) == true)
        {
            return "A caller claim using the role claim URI conflates separate Screenplay roles and claims in Arc.";
        }

        if (specification.When is { } returning && context.Commands.TryGetValue(returning.Command, out var responding) &&
            !HasComparableResponse(context, specification, responding))
        {
            return CompositeResponseRejection;
        }

        return "The scenario cannot preserve all of its declared fixtures or expectations.";
    }

    // A sourceless given fact has its own code so it is not lost in the generic STAGE-ESM-011 bucket.
    static ArtifactRenderDiagnostic RefusalDiagnostic(SemanticApplicationContext context, SemanticSpecification specification, string reason)
    {
        if (reason != SourcelessGivenReason)
        {
            return new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {reason}", specification.Id);
        }

        // Projected facts lower a sourceless 'when append' into the givens, so the event may be the appended one.
        var (role, contract) = specification.GivenEvents.FirstOrDefault(_ => _.EventSource is null) is { } given
            ? ("given", given.EventContract)
            : ("appended", specification.WhenAppended?.EventContract);
        var eventName = contract is { } id && context.Events.TryGetValue(id, out var @event) ? $"'{@event.Name}'" : "an event";
        return new(
            SourcelessGivenCode,
            ArtifactRenderDiagnosticSeverity.Error,
            $"Specification '{specification.Name}' cannot render: {role} event {eventName} has no event source. {reason}",
            specification.Id);
    }
}
