// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    internal static SemanticReactionSpecification SelectReaction(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var candidates = ReactionCandidates(context, specification).ToArray();

        return specification.ThenNoEvents || specification.WhenRedelivered is not null ? candidates.Single() : candidates.Single(candidate => candidate.Fires);
    }

    static IEnumerable<SemanticReactionSpecification> ReactionCandidates(SemanticApplicationContext context, SemanticSpecification specification) =>
        SemanticReactionSpecification.Candidates(context, SemanticReactionSpecification.Input(specification))
            .Where(candidate => specification.WhenRedelivered is not { } redelivery || candidate.Reaction.Id == redelivery.Reaction);

    static void ValidateReactionAppend(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (ReactionAppendRejection(context, specification, diagnostics) is not { } reason) return;
        diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {reason}", specification.Id));
    }

    static string? ReactionAppendRejection(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (ReactionFixtureRejection(context, specification) is { } reason) return reason;
        var append = SemanticReactionSpecification.Input(specification);
        var candidates = ReactionCandidates(context, specification);
        var active = (specification.ThenNoEvents || specification.WhenRedelivered is not null ? candidates : candidates.Where(candidate => candidate.Fires)).ToArray();
        if (active.Length != 1)
        {
            if (specification.WhenRedelivered is not null)
                return "A redelivery must select exactly one event trigger on its named reaction; fan-out requires a settlement harness.";

            return specification.ThenNoEvents
                ? "A no-events append must have exactly one observer trigger; fan-out and no-observer appends require an event-log delta and settlement harness."
                : "The append must select one reaction trigger, not fan-out or a non-reaction append.";
        }

        if (active[0].Trigger.Invokes.Any(invocation => !invocation.OnRefused.IsEmpty))
        {
            return specification.WhenRedelivered is not null
                ? "Redelivery with invocation refusal branches requires a context-preserving refusal settlement harness."
                : RefusalScenarioRejection(context, specification, active[0]);
        }

        if (specification.ThenNoEvents && !active[0].Trigger.Invokes.IsEmpty)
        {
            return "No-events append specifications support production-only reactions; invoked commands require a settlement harness.";
        }

        var selected = active[0];
        if (UsesReactionContext(selected.Reaction, append.EventContract))
            return "Reaction specifications cannot supply deterministic $context values.";

        return ReactionEffectRejection(context, specification, selected) ?? ReactionOutcomeRejection(context, specification, selected, diagnostics);
    }

    static string? ReactionFixtureRejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var append = SemanticReactionSpecification.Input(specification);
        if (specification.WhenRedelivered is not null && specification.GivenEvents.Any(given =>
            !HasRenderableReactionSource(context, given.EventSource) ||
            (given.Route is not null && !SemanticSpecificationRouteRendering.CanFormat(given.Route, context))))
            return "Redelivery history requires losslessly representable event sources and portable stream identities.";
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

    static string? RefusalScenarioRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        if (specification.ThenErrors.Length > 1 || !HasRenderableErrors(context, specification, null))
        {
            return "Refusal scenarios cannot faithfully assert coded, multiple or localized rejection expectations; only one uncoded portable error message is supported.";
        }

        var trigger = selected.Trigger;
        if (!trigger.Produces.IsEmpty)
        {
            return "Refusal scenarios do not support mixed direct productions and invocations.";
        }

        if (specification.WhenAppended!.EventSource is null || specification.WhenAppended.Route is not null || specification.GivenEvents.Any(given => given.Route is not null))
        {
            return "Refusal scenarios require an explicit append source and unrouted fixtures.";
        }

        if (UsesReactionContext(selected.Reaction, trigger.Source))
        {
            return "Refusal scenarios cannot supply deterministic $context values.";
        }

        var commands = trigger.Invokes.Select(invocation => context.Commands[invocation.Command]).ToArray();
        if (commands.Any(command => command.Properties.Any(property => property.IsGenerated || !IsRefusalScalarType(property.Type))))
        {
            return "Refusal scenarios require scalar commands without generated fixtures.";
        }

        var products = SemanticReactionProductions.For(trigger).Concat(commands.SelectMany(command => command.Produces)).ToArray();
        if (products.Any(produced => IsReactionConstraintTarget(context, produced.EventContract)))
        {
            return "Refusal scenarios do not yet reproduce constraint-target command or branch outputs.";
        }

        if (EventReactionTriggers(context).Any(next => products.Any(produced => produced.EventContract == next.Source)))
        {
            return "Generated refusal scenarios do not settle deeper reaction cascades.";
        }

        if (specification.ThenEventsInAnyOrder || specification.ThenReturns is not null ||
            specification.ThenEvents.Any(expected => !HasRenderableProductionExpectation(context, expected)) ||
            products.Any(produced => context.Events[produced.EventContract].Properties.Any(property => !IsRefusalScalarType(property.Type))))
        {
            return "Refusal scenarios require ordered scalar event expectations without a response.";
        }

        return null;
    }

    static bool IsRefusalScalarType(SemanticTypeReference type) =>
        !type.IsCollection && !type.IsOptional && type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept;

    static bool HasReactionReadExpectations(SemanticSpecification specification) =>
        !specification.GivenReadModels.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty;

    static bool HasRenderableReactionFixtures(SemanticApplicationContext context, SemanticSpecification specification) =>
        HasRenderableGivenEvents(context, specification) && EventMatches(context, new(SemanticReactionSpecification.Input(specification).EventContract, SemanticReactionSpecification.Input(specification).Values));

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
        if (HasDeeperReactionCascade(context, specification, selected))
            return "Generated reactor scenarios do not settle deeper reaction cascades.";

        return selected.InvokedProductions(specification, context).Any(produced => IsReactionConstraintTarget(context, produced.EventContract))
            ? "An invoked command produces an append-time constraint target." : null;
    }

    static bool HasUnsupportedReactionEffects(SemanticReactionTrigger trigger) =>
        trigger.Invokes.Length > 1 || (!trigger.Invokes.IsEmpty && !trigger.Produces.IsEmpty);

    static bool LosesReactionDestination(SemanticSpecification specification, SemanticReactionTrigger trigger) =>
        SemanticReactionSpecification.Input(specification).EventSource is null && trigger.Produces.Any(produced => produced.Destination is null);

    static bool HasDeeperReactionCascade(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var products = selected.Trigger.Produces.Concat(selected.InvokedProductions(specification, context)).Select(produced => produced.EventContract).ToHashSet();

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
        (SemanticReactionProductions.For(trigger).Any(produced => produced.Destination is SemanticEventContextExpression || produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression)) ||
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
