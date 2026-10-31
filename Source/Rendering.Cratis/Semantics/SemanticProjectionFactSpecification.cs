// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Recognises a specification that only projects facts: events given as history, optionally one more event appended
/// directly, and the read model state that follows. No command runs and no reaction is asked to settle.
/// </summary>
/// <remarks>
/// Without seeds the append is the last history fact. With seeds it stays separate so the generated scenario
/// replaces the seeded keys' history before folding the append over their fixture state. Neither case needs
/// the reaction scenarios, which do not materialize read models.
/// </remarks>
internal static class SemanticProjectionFactSpecification
{
    /// <summary>
    /// Lowers a projection-only specification while retaining a seeded append's position after its fixtures.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The authored specification.</param>
    /// <param name="lowered">The specification with the appended event as its last given event and no action.</param>
    /// <returns>True when the specification asserts only projected read-model state over facts.</returns>
    public static bool TryLower(SemanticApplicationContext context, SemanticSpecification specification, [NotNullWhen(true)] out SemanticSpecification? lowered)
    {
        lowered = null;
        if (specification.When is not null || specification.WhenRedelivered is not null || specification.WhenCapture is not null ||
            specification.WhenTrigger is not null || specification.WhenClock is not null ||
            (specification.ThenReadModels.IsEmpty && specification.ThenAbsentReadModels.IsEmpty) || !specification.ThenEvents.IsEmpty || specification.ThenNoEvents ||
            specification.ThenEventsInAnyOrder || !specification.ThenQueries.IsEmpty || !specification.ThenErrors.IsEmpty ||
            specification.ThenDenied || specification.ThenReturns is not null ||
            HasUnsupportedSeedExpectation(specification))
        {
            return false;
        }

        if (specification.WhenAppended is not { } appended)
        {
            lowered = specification;
            return true;
        }

        // An event the specification's own slice reacts to would have to be settled; an event an append-time constraint governs would have to be refused.
        // Another slice's reactions do not run in this specification (Screenplay decision 0115).
        if (SemanticSpecificationAdmission.EventReactionTriggers(context, specification).Any(trigger => trigger.Source == appended.EventContract) ||
            context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == appended.EventContract)))
        {
            return false;
        }

        lowered = specification with { WhenAppended = appended };
        if (specification.GivenReadModels.IsEmpty) lowered = WithAppendedFact(lowered);
        return true;
    }

    internal static SemanticSpecification WithAppendedFact(SemanticSpecification specification) =>
        specification.WhenAppended is not { } appended ? specification : specification with
        {
            WhenAppended = null,
            GivenEvents = specification.GivenEvents.Add(new(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route })
        };

    static bool HasUnsupportedSeedExpectation(SemanticSpecification specification) =>
        specification.ThenAbsentReadModels.IsEmpty && specification.GivenReadModels.Any(seed => seed.Key is not SemanticCompositeValue);
}
