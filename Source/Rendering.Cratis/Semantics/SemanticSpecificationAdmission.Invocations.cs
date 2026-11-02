// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    /// <summary>Refuses invocation expectations outside the single-hop recording scenario, before any slice-specific lowering.</summary>
    /// <param name="context">The indexed application.</param>
    /// <param name="slice">The selected slice.</param>
    /// <param name="diagnostics">The admission diagnostics.</param>
    internal static void ValidateInvocations(SemanticApplicationContext context, SemanticSlice slice, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications)
        {
            if (specification.ThenInvocations.IsDefaultOrEmpty) continue;
            if (InvocationRejection(context, slice, specification) is not { } reason) continue;
            diagnostics.Add(new("STAGE-ESM-016", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render ThenInvocations: {reason}", specification.Id));
        }
    }

    static string? InvocationRejection(SemanticApplicationContext context, SemanticSlice slice, SemanticSpecification original)
    {
        if (!IsInvocationEntry(slice, original))
            return "invocation recording supports only an ordered direct append or lowered command cascade, not Translate, redelivery, capture or trigger entry points.";
        if (!TryInvocationScenario(context, original, out var specification))
            return "the command cannot lower to one observable reaction invocation.";
        if (specification.WhenAppended is not { } append)
            return "invocation recording requires an append input.";
        var fired = SemanticReactionSpecification.Candidates(context, specification, append).Where(candidate => candidate.Fires).ToArray();
        if (fired.Length != 1)
            return "invocation recording requires exactly one fired reaction.";
        if (!IsSingleRecordedInvocation(context, specification, fired[0]))
            return "invocation recording requires a declarative invoke-only reaction without native bodies, refusal branches, context mappings or deeper effects.";
        if (specification.ThenInvocations.Any(expected => !CanCompareInvocation(context, expected)))
            return "expected values require distinct non-generated scalar, concept or scalar collection command properties.";
        return null;
    }

    static bool HasInvocationOutcome(SemanticSpecification specification, bool reactionInvocation) =>
        reactionInvocation && !specification.ThenInvocations.IsDefaultOrEmpty;

    static bool AssertsForeignInvocation(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected) =>
        !specification.ThenInvocations.IsDefaultOrEmpty && InvokesForeignCommand(context, specification, selected);

    static bool HasMatchingProductionCount(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, bool foreignInvocationExpectations) =>
        foreignInvocationExpectations || specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty ||
        specification.ThenEvents.Length == SemanticProductionSelection.Taken(context.Application, command, specification).Length;

    static bool HasMatchingProductionContracts(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, bool foreignInvocationExpectations) =>
        foreignInvocationExpectations || specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty ||
        (specification.ThenEventsInAnyOrder
            ? specification.ThenEvents.GroupBy(_ => _.EventContract).All(group =>
                SemanticProductionSelection.Taken(context.Application, command, specification).Count(_ => _.EventContract == group.Key) == group.Count())
            : specification.ThenEvents.Select(_ => _.EventContract).SequenceEqual(SemanticProductionSelection.Taken(context.Application, command, specification).Select(_ => _.EventContract)));

    static bool IsInvocationEntry(SemanticSlice slice, SemanticSpecification specification) =>
        slice.Kind != SemanticSliceKind.Translate && specification.WhenRedelivered is null && specification.WhenCapture is null && specification.WhenTrigger is null && !specification.ThenEventsInAnyOrder;

    static bool TryInvocationScenario(SemanticApplicationContext context, SemanticSpecification original, out SemanticSpecification specification)
    {
        specification = original;
        if (original.When is null) return true;
        if (!CommandTriggersReaction(context, original)) return false;
        if (!SemanticReactionCascade.TryLower(context, original, out var cascade)) return false;
        specification = cascade.Reaction;
        return true;
    }

    static bool IsSingleRecordedInvocation(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var trigger = selected.Trigger;
        if (!IsDeclarativeInvocation(trigger)) return false;
        if (!trigger.Invokes[0].OnRefused.IsDefaultOrEmpty) return false;
        if (UsesReactionContext(selected.Reaction, trigger.Source)) return false;
        return !HasDeeperReactionCascade(context, specification, selected);
    }

    static bool CanCompareInvocation(SemanticApplicationContext context, SemanticSpecificationInvocation expected)
    {
        if (!context.Commands.TryGetValue(expected.Command, out var command)) return false;
        if (!HasDistinctInvocationValues(expected)) return false;
        return expected.Values.All(value => CanCompareInvocationValue(context, command, value));
    }

    static bool IsDeclarativeInvocation(SemanticReactionTrigger trigger) =>
        trigger.Invokes.Length == 1 && trigger.Produces.IsEmpty && trigger.RequirementId is null;

    static bool HasDistinctInvocationValues(SemanticSpecificationInvocation expected) =>
        expected.Values.Select(value => value.TargetProperty).Distinct().Count() == expected.Values.Length;

    static bool CanCompareInvocationValue(SemanticApplicationContext context, SemanticCommand command, SemanticPropertyValue value)
    {
        if (command.Properties.SingleOrDefault(property => property.Id == value.TargetProperty) is not { IsGenerated: false } property) return false;
        if (property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)) return false;
        return IsCompatible(value.Value, property.Type, context);
    }
}
