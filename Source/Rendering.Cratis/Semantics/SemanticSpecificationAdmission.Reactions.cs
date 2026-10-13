// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    internal static SemanticReactionSpecification SelectReaction(SemanticApplicationContext context, SemanticSpecification specification)
    {
        return SemanticReactionSpecification.Candidates(context, specification.WhenAppended!).Single(candidate => candidate.Fires);
    }

    static void ValidateReactionAppend(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (ReactionAppendRejection(context, specification) is not { } reason) return;
        diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {reason}", specification.Id));
    }

    static string? ReactionAppendRejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (ReactionFixtureRejection(context, specification) is { } reason) return reason;
        var append = specification.WhenAppended!;
        var candidates = SemanticReactionSpecification.Candidates(context, append).ToArray();
        var active = candidates.Where(candidate => candidate.Fires).ToArray();
        if (active.Length != 1)
            return "The append must select one reaction trigger, not fan-out or a non-reaction append.";
        var selected = active[0];
        if (UsesReactionContext(selected.Reaction, append.EventContract))
            return "Reaction specifications cannot supply deterministic $context values.";

        return ReactionEffectRejection(context, specification, selected) ?? ReactionOutcomeRejection(context, specification, selected);
    }

    static string? ReactionFixtureRejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var append = specification.WhenAppended!;
        if (!specification.GivenReadModels.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty)
            return "Reaction append scenarios do not materialize read-model, query or absence expectations.";
        if (!HasRenderableGivenEvents(context, specification) || !EventMatches(context, new(append.EventContract, append.Values)))
            return "The append or given events cannot be represented faithfully by a reactor scenario.";
        if (append.EventSource is { } source && (!IsScalar(source.Value) || !IsLosslessEventSource(context, source.Type)))
            return "The appended event source must be losslessly representable.";
        if (append.Route is { } route && !SemanticSpecificationRouteRendering.CanFormat(route, context))
            return "The append route's stream identity cannot be formatted portably.";

        return IsReactionConstraintTarget(context, append.EventContract)
            ? "The appended event is an append-time constraint target; a reactor scenario does not enforce that append." : null;
    }

    static string? ReactionEffectRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var trigger = selected.Trigger;
        if (trigger.Invokes.Length > 1 || (!trigger.Invokes.IsEmpty && !trigger.Produces.IsEmpty))
            return "Only production-only reactions or one invoked command are supported.";
        if (trigger.Produces.Any(produced => IsReactionConstraintTarget(context, produced.EventContract)))
            return "A produced event is an append-time constraint target; the scenario only records reactor effects.";
        if (specification.WhenAppended!.EventSource is null && trigger.Produces.Any(produced => produced.Destination is null))
            return "A sourceless append cannot preserve a production that omits 'for'.";
        var products = trigger.Produces.Select(produced => produced.EventContract)
            .Concat(trigger.Invokes.SelectMany(invocation => context.Commands[invocation.Command].Produces.Select(produced => produced.EventContract))).ToHashSet();
        if (EventReactionTriggers(context).Any(next => products.Contains(next.Source)))
            return "Generated reactor scenarios do not settle deeper reaction cascades.";
        return trigger.Invokes.SelectMany(invocation => context.Commands[invocation.Command].Produces).Any(produced => IsReactionConstraintTarget(context, produced.EventContract))
            ? "An invoked command produces an append-time constraint target." : null;
    }

    static string? ReactionOutcomeRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        if (selected.Trigger.Invokes.IsEmpty)
            return ProductionExpectationsMatch(context, specification, selected) ? null : "The production expectations cannot be represented by this reactor scenario.";
        var lowered = selected.Lower(specification, context);

        return CanRenderSpecification(context, lowered, reactionInvocation: true) ? null : RejectionReason(context, lowered);
    }

    static bool IsReactionConstraintTarget(SemanticApplicationContext context, SemanticId @event) =>
        context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == @event) || selected.Constraint.ReleasedBy.Contains(@event));

    static bool UsesReactionContext(SemanticReaction reaction, SemanticId source) => reaction.Triggers.Any(trigger => trigger.Source == source &&
        (trigger.Produces.Any(produced => produced.Destination is SemanticEventContextExpression || produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression)) ||
        trigger.Invokes.Any(invocation => invocation.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))));

    static bool ProductionExpectationsMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        if (specification.ThenDenied || !specification.ThenErrors.IsEmpty || specification.ThenReturns is not null) return false;
        var produces = selected.Trigger.Produces;

        return specification.ThenEvents.Length == produces.Length && specification.ThenEvents.All(expected => EventMatches(context, expected) &&
            expected.Route is null && (expected.EventSource is not { } source || (IsScalar(source.Value) && IsLosslessEventSource(context, source.Type)))) &&
            (specification.ThenEventsInAnyOrder
                ? specification.ThenEvents.GroupBy(expected => expected.EventContract).All(group => produces.Count(produced => produced.EventContract == group.Key) == group.Count())
                : specification.ThenEvents.Select(expected => expected.EventContract).SequenceEqual(produces.Select(produced => produced.EventContract)));
    }
}
