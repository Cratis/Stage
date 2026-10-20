// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks specifications that assert the read-model state projected from facts alone.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static bool CanRenderProjectedFacts(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var facts = SemanticProjectionFactSpecification.WithAppendedFact(specification);
        return CanSeedProjectedAbsence(context, specification) &&
            (!facts.GivenEvents.IsEmpty || !specification.GivenReadModels.IsEmpty || specification.ThenReadModels.IsEmpty) &&
            HasRenderableGivenEvents(context, facts) && GivenKeysMatchProjectedProperties(context, facts) && !GivenEventsViolateConstraints(context, facts) &&
            specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
                (SemanticProjectionSpecificationSeeds.Contains(specification, expected) || IsProjectedFromFacts(context, facts, expected))) &&
            specification.ThenAbsentReadModels.All(expected => CanAssertAbsence(context, specification, expected));
    }

    static string ProjectedFactsRejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        specification = SemanticProjectionFactSpecification.WithAppendedFact(specification);
        if (specification.GivenEvents.IsEmpty && specification.GivenReadModels.IsEmpty && !specification.ThenReadModels.IsEmpty)
        {
            return "A read model asserted without an action needs the events that project it.";
        }

        if (!HasRenderableGivenEvents(context, specification))
        {
            return "The events cannot be represented faithfully: each needs an explicit, losslessly representable event source and complete values.";
        }

        if (!CanSeedProjectedAbsence(context, specification) || specification.ThenAbsentReadModels.Any(expected => !CanAssertAbsence(context, specification, expected)))
        {
            return AbsenceRejection;
        }

        if (specification.ThenReadModels.Any(expected => !IsProjectedFromFacts(context, specification, expected)))
        {
            return "A projected read model needs a projection and an event it handles on the asserted instance; a reducer or an instance no given event reaches cannot be proven here.";
        }

        return "The scenario cannot preserve all of its declared fixtures or expectations.";
    }

    // A projection builds the instance the specification names when one of its events was given on that instance:
    // by event source, by the event property the block is keyed on, or by the constant key the block declares.
    static bool IsProjectedFromFacts(SemanticApplicationContext context, SemanticSpecification specification, SemanticSpecificationReadModel expected)
    {
        if (context.Reducers.Any(reducer => reducer.ReadModel == expected.ReadModel) ||
            context.Projections.Values.SingleOrDefault(projection => projection.ReadModel == expected.ReadModel) is not { } projection)
        {
            return false;
        }

        var froms = projection.Scope is { } scope
            ? scope.From.Select(from => (from.EventContract, from.Key))
            : projection.Transitions.Select(transition => (transition.EventContract, Key: SemanticProjectionKey.EventSourceIdentity));

        return specification.GivenEvents.Any(given => froms.Any(from => from.EventContract == given.EventContract &&
            KeyIdentifies(from.Key, given, expected.Key)));
    }

    static bool KeyIdentifies(SemanticProjectionKey key, SemanticSpecificationEvent given, SemanticValue expected) =>
        SemanticProjectionSpecificationSeeds.KeyIdentifies(key, given, expected);
}
