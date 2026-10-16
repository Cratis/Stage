// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Splits a command specification whose events trigger one command-invoking reaction into the two halves a generated scenario can run.
/// </summary>
/// <remarks>
/// A generated command scenario does not settle reactions. The cascade is preserved by running the triggering command in its own
/// scenario, and by appending the event it produces to the reactor scenario that executes the invoked command.
/// </remarks>
/// <param name="Reaction">The append specification that runs the reactor and the invoked command.</param>
/// <param name="Trigger">The triggering command's own specification, or null when the cascade is refused and the original lists no trigger facts.</param>
internal sealed record SemanticReactionCascade(SemanticSpecification Reaction, SemanticSpecification? Trigger)
{
    internal const string TriggerSuffix = "Trigger";

    /// <summary>
    /// Splits a cascading command specification.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <param name="cascade">The two halves.</param>
    /// <returns>Whether exactly one produced event triggers exactly one invoke-only reaction.</returns>
    internal static bool TryLower(SemanticApplicationContext context, SemanticSpecification specification, out SemanticReactionCascade cascade)
    {
        cascade = null!;
        if (specification.When is not { } action || specification.ThenEventsInAnyOrder || specification.ThenDenied || specification.ThenReturns is not null ||
            !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty || !context.Commands.TryGetValue(action.Command, out var command))
        {
            return false;
        }

        var taken = SemanticProductionSelection.Taken(command, specification);
        var triggers = taken.Select(produced => (Produced: produced, Append: Append(context, specification, command, produced)))
            .Where(item => item.Append is not null)
            .SelectMany(item => SemanticReactionSpecification.Candidates(context, item.Append!).Where(candidate => candidate.Fires).Select(candidate => (item.Append, Selection: candidate)))
            .ToArray();
        if (triggers.Length != 1 || triggers[0].Selection.Trigger.Invokes.Length != 1 || !triggers[0].Selection.Trigger.Produces.IsEmpty)
        {
            return false;
        }

        var accepted = specification.ThenErrors.IsEmpty;
        if (accepted && specification.ThenEvents.Length < taken.Length) return false;

        var reaction = specification with
        {
            When = null,
            WhenAppended = triggers[0].Append,
            GivenCaller = null,
            ThenEvents = accepted ? [.. specification.ThenEvents.Skip(taken.Length)] : []
        };
        var trigger = accepted
            ? specification with { Name = specification.Name + TriggerSuffix, ThenEvents = [.. specification.ThenEvents.Take(taken.Length)] }
            : null;
        cascade = new(reaction, trigger);

        return true;
    }

    static SemanticSpecificationAppend? Append(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, SemanticProducedEvent produced)
    {
        if (produced.Route is not null || command.Route is not null || SemanticDestinations.Of(command, produced) is not SemanticResolvedExpression) return null;

        var inputs = specification.When!.Values.Concat(specification.When.GeneratedValues).ToArray();
        var values = new List<SemanticPropertyValue>();
        foreach (var property in context.Events[produced.EventContract].Properties)
        {
            var mapping = produced.Mappings.Single(candidate => candidate.TargetProperty == property.Id);
            var value = mapping.Source switch
            {
                SemanticValueExpression literal => literal.Value,
                SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } resolved =>
                    inputs.FirstOrDefault(input => input.TargetProperty == resolved.Target)?.Value,
                _ => null
            };
            if (value is null) return null;

            values.Add(new(property.Id, value));
        }

        return new(produced.EventContract, [.. values]) { EventSource = SemanticDestinations.ForSpecification(specification, command, produced) };
    }
}
