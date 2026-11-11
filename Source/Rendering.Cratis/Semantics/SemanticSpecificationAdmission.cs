// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    /// The code that refuses a specification whose given or appended event has an observable missing source.
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
        foreach (var specification in AdmissibleSpecifications(context, slice, diagnostics))
        {
            ValidateSpecification(context, specification, diagnostics);
        }
    }

    static IEnumerable<SemanticSpecification> AdmissibleSpecifications(SemanticApplicationContext context, SemanticSlice slice, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        var withoutCascades = WithoutReactionCascades(context, slice, diagnostics);
        var withoutBodies = SemanticCommandBodyAdmission.WithoutBodySpecifications(withoutCascades, context, diagnostics);
        var withoutClocks = SemanticOpaqueBodyDependencies.WithoutUncontrolledClockSpecifications(withoutBodies, context, diagnostics);
        return WithoutDivergentRootJoins(withoutClocks, context, diagnostics);
    }

    static void ValidateSpecification(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (TryRefuseUnboundEventMetadata(context, specification, diagnostics)) return;
        if (TryValidateProjectedFacts(context, specification, diagnostics)) return;
        if (TryRefuseAppend(context, specification, diagnostics)) return;

        ValidateActionSpecification(context, specification, diagnostics);
    }

    static bool TryRefuseAppend(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (AppendRefusedConstraint(context, specification) is not { } refusing) return false;
        if (AppendRefusalRejection(context, specification, refusing) is { } refusal)
        {
            diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {refusal}", specification.Id));
        }

        return true;
    }

    // Only the declaring slice's own reactions can make a command specification a cascade (Screenplay decision 0115).
    internal static bool CommandTriggersReaction(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action) return false;
        if (!context.Commands.TryGetValue(action.Command, out var command)) return false;

        return EventReactionTriggers(context, specification).Any(trigger => SemanticProductionSelection.Taken(context.Application, command, specification).Any(produced => produced.EventContract == trigger.Source));
    }

    internal static bool IsReactionConstraintTarget(SemanticApplicationContext context, SemanticId @event) =>
        context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == @event) || selected.Constraint.ReleasedBy.Contains(@event));

    internal static bool TriggerValuesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        CommandValuesMatch(context, specification, command);

    static bool TryValidateProjectedFacts(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!SemanticProjectionFactSpecification.TryLower(context, specification, out var facts)) return false;
        if (!CanRenderProjectedFacts(context, facts))
        {
            diagnostics.Add(RefusalDiagnostic(context, specification, ProjectedFactsRejection(context, facts)));
        }

        return true;
    }

    static void ValidateActionSpecification(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (specification.WhenAppended is not null || specification.WhenRedelivered is not null)
        {
            ValidateReactionAppend(context, specification, diagnostics);
            return;
        }
        var valid = CanRenderSpecification(context, specification);

        if (!valid)
        {
            var reason = RejectionReason(context, specification);
            diagnostics.Add(RefusalDiagnostic(context, specification, reason));
        }
    }

    static bool CanRenderSpecification(SemanticApplicationContext context, SemanticSpecification specification, bool reactionInvocation = false, bool foreignInvocationExpectations = false) =>
        HasRenderableQueryExpectations(context, specification) &&
        (CanRenderQueryOnly(context, specification) ||
            (HasRenderableCallerAndCommand(context, specification, out var command, reactionInvocation) &&
                HasRenderableFixtures(context, specification, command!) &&
                HasOneOutcome(specification, reactionInvocation) &&
                HasRenderableProductions(context, specification, command!, foreignInvocationExpectations) &&
                HasRenderableStateExpectations(context, specification) &&
                HasRenderableOutcome(context, specification, command!)));

    static bool HasRenderableFixtures(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        HasRenderableOrderedGivenEvents(context, specification) && GivenKeysMatchProjectedProperties(context, specification) &&
        !AssertsUncontrolledOccurrence(context.Application, specification, command) &&
        !GivenEventsViolateConstraints(context, specification) && CanSeedProjectedAbsence(context, specification) &&
        CommandValuesMatch(context, specification, command);

    static bool HasRenderableProductions(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, bool foreignInvocationExpectations) =>
        HasSupportedCounts(specification) &&
        HasMatchingProductionCount(context, specification, command, foreignInvocationExpectations) &&
        specification.ThenEvents.All(expected => EventMatches(context, expected)) &&
        HasMatchingProductionContracts(context, specification, command, foreignInvocationExpectations);

    static bool HasRenderableStateExpectations(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
            HasExpectedProjectionEvent(context, specification, expected) &&
            ReducerRejectionReason(context, expected.ReadModel) is null) &&
        specification.ThenAbsentReadModels.All(expected => CanAssertAbsence(context, specification, expected)) &&
        specification.ThenQueries.All(expected => QueryMatches(context, expected) &&
            !HasReducer(context, context.Queries[expected.Query].ReadModel) &&
            expected.Results.All(result => HasExpectedProjectionEvent(context, specification, result)));

    static bool HasRenderableOutcome(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        HasRenderableErrors(context, specification, command) &&
        HasRenderableEventSources(context, specification, command) &&
        ProtectedQuerySourcesMatch(context, specification, command) &&
        HasComparableResponse(context, specification, command);

    static IEnumerable<SemanticSpecification> WithoutReactionCascades(SemanticApplicationContext context, SemanticSlice slice, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in WithRenderableGeneratedValues(context, slice.Specifications.Select(specification => specification.WithPinnedActionValues()), diagnostics))
        {
            if (TryRefuseDeniedOperation(context, specification, diagnostics))
            {
                continue;
            }
            if (!CommandTriggersReaction(context, specification) || SemanticReactionCascade.IsRejectedByTrigger(context, specification))
            {
                yield return specification;
                continue;
            }

            ReportCascade(context, specification, diagnostics);
        }
    }

    static void ReportCascade(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (SemanticReactionCascade.TryLower(context, specification, out var cascade))
        {
            ValidateCascade(context, cascade, diagnostics);
            return;
        }

        diagnostics.Add(new(
            "STAGE-ESM-011",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Specification '{specification.Name}' cannot render: the command produces an event that triggers a reaction; generated CommandScenario specifications do not settle reactions yet.",
            specification.Id));
    }

    static void ValidateCascade(SemanticApplicationContext context, SemanticReactionCascade cascade, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (TryRefuseDeniedOperation(context, cascade.Reaction, diagnostics)) return;
        ValidateReactionAppend(context, cascade.Reaction, diagnostics);
        if (cascade.Trigger is { } trigger && !CanRenderSpecification(context, trigger))
        {
            diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{cascade.Reaction.Name}' cannot render: its triggering command specification cannot be preserved ({RejectionReason(context, trigger)}).", cascade.Reaction.Id));
        }
    }
}
