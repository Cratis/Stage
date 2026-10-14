// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    internal static SemanticReactionSpecification SelectReaction(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var candidates = SemanticReactionSpecification.Candidates(context, specification.WhenAppended!).ToArray();

        return specification.ThenNoEvents ? candidates.Single() : candidates.Single(candidate => candidate.Fires);
    }

    static void ValidateReactionAppend(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (ReactionAppendRejection(context, specification, diagnostics) is not { } reason) return;
        diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {reason}", specification.Id));
    }

    static string? ReactionAppendRejection(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (ReactionFixtureRejection(context, specification) is { } reason) return reason;
        var append = specification.WhenAppended!;
        var candidates = SemanticReactionSpecification.Candidates(context, append);
        var active = (specification.ThenNoEvents ? candidates : candidates.Where(candidate => candidate.Fires)).ToArray();
        if (active.Length != 1)
            return specification.ThenNoEvents
                ? "A no-events append must have exactly one observer trigger; fan-out and no-observer appends require an event-log delta and settlement harness."
                : "The append must select one reaction trigger, not fan-out or a non-reaction append.";
        if (specification.ThenNoEvents && !active[0].Trigger.Invokes.IsEmpty)
            return "No-events append specifications support production-only reactions; invoked commands require a settlement harness.";
        var selected = active[0];
        if (UsesReactionContext(selected.Reaction, append.EventContract))
            return "Reaction specifications cannot supply deterministic $context values.";

        return ReactionEffectRejection(context, specification, selected) ?? ReactionOutcomeRejection(context, specification, selected, diagnostics);
    }

    static string? ReactionFixtureRejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var append = specification.WhenAppended!;
        if (HasReactionReadExpectations(specification))
            return "Reaction append scenarios do not materialize read-model, query or absence expectations.";
        if (!HasRenderableReactionFixtures(context, specification))
            return "The append or given events cannot be represented faithfully by a reactor scenario.";
        if (!HasRenderableReactionSource(context, append.EventSource))
            return "The appended event source must be losslessly representable.";
        if (!HasRenderableReactionRoute(context, append))
            return "The append route's stream identity cannot be formatted portably.";

        return IsReactionConstraintTarget(context, append.EventContract)
            ? "The appended event is an append-time constraint target; a reactor scenario does not enforce that append." : null;
    }

    static bool HasReactionReadExpectations(SemanticSpecification specification) =>
        !specification.GivenReadModels.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty;

    static bool HasRenderableReactionFixtures(SemanticApplicationContext context, SemanticSpecification specification) =>
        HasRenderableGivenEvents(context, specification) && EventMatches(context, new(specification.WhenAppended!.EventContract, specification.WhenAppended.Values));

    static bool HasRenderableReactionSource(SemanticApplicationContext context, SemanticEventSourceIdentity? source) =>
        source is null || (IsScalar(source.Value) && IsLosslessEventSource(context, source.Type));

    static bool HasRenderableReactionRoute(SemanticApplicationContext context, SemanticSpecificationAppend append) =>
        append.Route is null || SemanticSpecificationRouteRendering.CanFormat(append.Route, context);

    static string? ReactionEffectRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var trigger = selected.Trigger;
        if (HasUnsupportedReactionEffects(trigger))
            return "Only production-only reactions or one invoked command are supported.";
        if (trigger.Produces.Any(produced => IsReactionConstraintTarget(context, produced.EventContract)))
            return "A produced event is an append-time constraint target; the scenario only records reactor effects.";
        if (LosesReactionDestination(specification, trigger))
            return "A sourceless append cannot preserve a production that omits 'for'.";
        if (HasDeeperReactionCascade(context, trigger))
            return "Generated reactor scenarios do not settle deeper reaction cascades.";

        return InvokedProductions(context, trigger).Any(produced => IsReactionConstraintTarget(context, produced.EventContract))
            ? "An invoked command produces an append-time constraint target." : null;
    }

    static bool HasUnsupportedReactionEffects(SemanticReactionTrigger trigger) =>
        trigger.Invokes.Length > 1 || (!trigger.Invokes.IsEmpty && !trigger.Produces.IsEmpty);

    static bool LosesReactionDestination(SemanticSpecification specification, SemanticReactionTrigger trigger) =>
        specification.WhenAppended!.EventSource is null && trigger.Produces.Any(produced => produced.Destination is null);

    static IEnumerable<SemanticProducedEvent> InvokedProductions(SemanticApplicationContext context, SemanticReactionTrigger trigger) =>
        trigger.Invokes.SelectMany(invocation => context.Commands[invocation.Command].Produces);

    static bool HasDeeperReactionCascade(SemanticApplicationContext context, SemanticReactionTrigger trigger)
    {
        var products = trigger.Produces.Concat(InvokedProductions(context, trigger)).Select(produced => produced.EventContract).ToHashSet();

        return EventReactionTriggers(context).Any(next => products.Contains(next.Source));
    }

    static string? ReactionOutcomeRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (selected.Trigger.Invokes.IsEmpty)
            return ProductionExpectationsMatch(context, specification, selected) ? null : "The production expectations cannot be represented by this reactor scenario.";
        var lowered = selected.Lower(specification, context);

        // The original append has no command action. Apply the command generation safeguard only after lowering.
        if (!WithRenderableGeneratedValues(context, [lowered], diagnostics).Any()) return null;

        return CanRenderSpecification(context, lowered, reactionInvocation: true) ? null : RejectionReason(context, lowered);
    }

    static bool IsReactionConstraintTarget(SemanticApplicationContext context, SemanticId @event) =>
        context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == @event) || selected.Constraint.ReleasedBy.Contains(@event));

    static bool UsesReactionContext(SemanticReaction reaction, SemanticId source) => reaction.Triggers.Any(trigger => trigger.Source == source &&
        (trigger.Produces.Any(produced => produced.Destination is SemanticEventContextExpression || produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression)) ||
        trigger.Invokes.Any(invocation => invocation.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))));

    static bool ProductionExpectationsMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected) =>
        HasOnlyProductionOutcomes(specification) && (specification.ThenNoEvents || specification.ThenEvents.Length == selected.Trigger.Produces.Length) &&
        specification.ThenEvents.All(expected => HasRenderableProductionExpectation(context, expected)) &&
        (specification.ThenNoEvents || ProductionContractsMatch(specification, selected.Trigger));

    static bool HasOnlyProductionOutcomes(SemanticSpecification specification) =>
        !specification.ThenDenied && specification.ThenErrors.IsEmpty && specification.ThenReturns is null;

    static bool HasRenderableProductionExpectation(SemanticApplicationContext context, SemanticSpecificationEvent expected) =>
        EventMatches(context, expected) && expected.Route is null && HasRenderableReactionSource(context, expected.EventSource);

    static bool ProductionContractsMatch(SemanticSpecification specification, SemanticReactionTrigger trigger) => specification.ThenEventsInAnyOrder
        ? specification.ThenEvents.GroupBy(expected => expected.EventContract).All(group => trigger.Produces.Count(produced => produced.EventContract == group.Key) == group.Count())
        : specification.ThenEvents.Select(expected => expected.EventContract).SequenceEqual(trigger.Produces.Select(produced => produced.EventContract));
}
