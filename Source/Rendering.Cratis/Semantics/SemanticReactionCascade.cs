// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Splits a command specification whose events trigger one command-invoking reaction into the two halves a generated scenario can run.
/// </summary>
/// <remarks>
/// A generated command scenario does not settle reactions. The cascade is preserved by running the triggering command in its own
/// scenario, and by appending the event it produces to the reactor scenario that executes the invoked command. A rejected cascade
/// drops the trigger half, so it is lowered only when the triggering command cannot reject on its own; otherwise it stays refused.
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

        var taken = SemanticProductionSelection.Taken(context.Application, command, specification);
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

        // A rejected cascade drops the trigger half, so it is only faithful when the triggering command cannot itself be the one that rejects.
        if (!accepted && (CanReject(context, command) || !SemanticSpecificationAdmission.TriggerValuesMatch(context, specification, command))) return false;

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

    /// <summary>
    /// Gets whether a rejection the specification expects can only come from the triggering command itself, so no event is appended and no reaction is set off.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>Whether the caller is denied by the triggering command's authorization, or every expected error is a literal message the triggering command declares and no command a reaction invokes declares.</returns>
    internal static bool IsRejectedByTrigger(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action || !specification.ThenEvents.IsEmpty || !context.Commands.TryGetValue(action.Command, out var command))
        {
            return false;
        }

        // The caller is denied before the command runs, so no event is appended and no reaction is set off.
        if (specification.ThenDenied) return command.Authorization is not null;

        if (specification.ThenErrors.IsEmpty || specification.ThenErrors.Any(error => error.Code is not null || error.Message is null))
        {
            return false;
        }

        var own = Messages(context, command);
        var triggered = SemanticSpecificationAdmission.EventReactionTriggers(context)
            .Where(trigger => command.Produces.Any(produced => produced.EventContract == trigger.Source))
            .SelectMany(trigger => trigger.Invokes)
            .Select(invocation => context.Commands[invocation.Command])
            .SelectMany(invoked => Messages(context, invoked))
            .ToHashSet(StringComparer.Ordinal);

        var absences = ReadAbsences(context, command);
        var triggeredAbsences = SemanticSpecificationAdmission.EventReactionTriggers(context)
            .Where(trigger => command.Produces.Any(produced => produced.EventContract == trigger.Source))
            .SelectMany(trigger => trigger.Invokes)
            .SelectMany(invocation => ReadAbsences(context, context.Commands[invocation.Command]))
            .ToHashSet();

        return specification.ThenErrors.All(error =>
            (own.Contains(error.Message!) && !triggered.Contains(error.Message!)) ||
            absences.Any(read => IsAbsence(error.Message!, read) && !triggeredAbsences.Contains(read)) ||
            IsOwnKeyedAbsence(context, command, specification, error.Message!) ||
            ViolatesOwnConcept(context, command, specification, error.Message!));
    }

    // A reaction's command reading the same view under the same key reads after the triggering command did. If the triggering
    // read found the instance and nothing the cascade appends removes it, the later read finds it too, so the absence is the trigger's.
    static bool IsOwnKeyedAbsence(SemanticApplicationContext context, SemanticCommand command, SemanticSpecification specification, string message) =>
        !command.Reads.IsDefaultOrEmpty && command.Reads.Any(read =>
            specification.When!.Values.FirstOrDefault(input => input.TargetProperty == read.Key)?.Value is SemanticTextValue key &&
            message == $"{context.ReadModels[read.ReadModel].Name} '{key.Value}' does not exist." &&
            !CascadeEvents(context, command).Any(@event => CanRemove(context, read.ReadModel, @event)));

    // Every event the triggering command can set off: its own productions, and to a fixpoint every reaction any of those events
    // triggers, through reaction productions, refusal-branch productions and invoked commands' productions.
    static HashSet<SemanticId> CascadeEvents(SemanticApplicationContext context, SemanticCommand command)
    {
        var triggers = SemanticSpecificationAdmission.EventReactionTriggers(context).ToArray();
        var events = command.Produces.Select(produced => produced.EventContract).ToHashSet();
        var commands = new HashSet<SemanticId> { command.Id };
        var reacted = new HashSet<int>();
        bool changed;
        do
        {
            changed = false;
            for (var index = 0; index < triggers.Length; index++)
            {
                var trigger = triggers[index];
                if (!events.Contains(trigger.Source) || !reacted.Add(index)) continue;
                changed = true;
                foreach (var produced in trigger.Produces) events.Add(produced.EventContract);
                foreach (var invocation in trigger.Invokes)
                {
                    foreach (var produced in invocation.OnRefused.SelectMany(refusal => refusal.Produces)) events.Add(produced.EventContract);
                    if (!commands.Add(invocation.Command)) continue;
                    foreach (var produced in context.Commands[invocation.Command].Produces) events.Add(produced.EventContract);
                }
            }
        }
        while (changed);

        return events;
    }

    // Only a removal can make an instance disappear; flat transitions create or update it. A command read is admitted only
    // over a view built by projections with flat removals (no reducer, join or child scope), so the scope's removals are all.
    static bool CanRemove(SemanticApplicationContext context, SemanticId readModel, SemanticId @event) =>
        context.Projections.Values.Any(projection => projection.ReadModel == readModel && projection.Scope is { } scope &&
            scope.Removals.Any(removal => removal.EventContract == @event));

    // A message a reaction's command also carries is still the triggering command's own when the specification's
    // literal input breaks a concept rule of that very command: binding fails before any event, so no reaction runs.
    static bool ViolatesOwnConcept(SemanticApplicationContext context, SemanticCommand command, SemanticSpecification specification, string message)
    {
        foreach (var input in specification.When!.Values)
        {
            var property = command.Properties.FirstOrDefault(candidate => candidate.Id == input.TargetProperty);
            if (property is null || property.Type.Kind != SemanticTypeReferenceKind.Concept || !context.Concepts.TryGetValue(property.Type.Target, out var concept)) continue;

            try
            {
                if (SemanticValidationRules.Check(concept, input.Value).Any(check => check.Status == SemanticRuleStatus.Failed && check.Message == message)) return true;
            }
            catch (InvalidSemanticContract)
            {
                // A value the rule cannot judge is not a proven rejection.
            }
        }

        return false;
    }

    // A protected read that finds nothing refuses the command before it appends: "<ReadModel> '<key>' does not exist."
    static IEnumerable<string> ReadAbsences(SemanticApplicationContext context, SemanticCommand command) =>
        command.Reads.IsDefaultOrEmpty ? [] : command.Reads.Select(read => context.ReadModels[read.ReadModel].Name);

    static bool IsAbsence(string message, string readModel) =>
        message.StartsWith(readModel + " '", StringComparison.Ordinal) && message.EndsWith("' does not exist.", StringComparison.Ordinal);

    static HashSet<string> Messages(SemanticApplicationContext context, SemanticCommand command)
    {
        var messages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in command.Validations.Select(rule => rule.Message).Concat(command.Requirements.Select(requirement => requirement.Message)))
        {
            if (message is not null) messages.Add(message);
        }

        foreach (var property in command.Properties) CollectConceptMessages(context, property.Type, [], messages);

        return messages;
    }

    static void CollectConceptMessages(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited, HashSet<string> messages)
    {
        switch (type.Kind)
        {
            case SemanticTypeReferenceKind.Concept:
                foreach (var rule in context.Concepts[type.Target].Validations.IsDefaultOrEmpty ? [] : context.Concepts[type.Target].Validations)
                {
                    if (rule.Message is not null) messages.Add(rule.Message);
                }

                break;
            case SemanticTypeReferenceKind.CompositeType when visited.Add(type.Target):
                foreach (var property in context.Types[type.Target].Properties) CollectConceptMessages(context, property.Type, visited, messages);
                break;
        }
    }

    static bool CanReject(SemanticApplicationContext context, SemanticCommand command) =>
        !command.Validations.IsDefaultOrEmpty || !command.Requirements.IsDefaultOrEmpty || !command.Reads.IsDefaultOrEmpty ||
        command.Route is not null || command.Authorization is not null || command.Properties.Any(property => property.IsGenerated) ||
        command.Produces.Any(produced => produced.Route is not null || SemanticSpecificationAdmission.IsReactionConstraintTarget(context, produced.EventContract)) ||
        command.Properties.Any(property => CarriesValidation(context, property.Type, []));

    static bool CarriesValidation(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited) => type.Kind switch
    {
        SemanticTypeReferenceKind.Concept => !context.Concepts[type.Target].Validations.IsDefaultOrEmpty,
        SemanticTypeReferenceKind.CompositeType => visited.Add(type.Target) &&
            context.Types[type.Target].Properties.Any(property => CarriesValidation(context, property.Type, visited)),
        _ => false
    };

    static SemanticSpecificationAppend? Append(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, SemanticProducedEvent produced)
    {
        if (produced.Route is not null || command.Route is not null || SemanticDestinations.Of(command, produced) is not SemanticResolvedExpression destination) return null;

        var inputs = specification.When!.Values.Concat(specification.When.GeneratedValues).ToArray();

        // An unpinned generated destination has no value in the specification, so the cascade cannot be split.
        var stated = command.Produces.Length == 1 && SemanticDestinations.Explicit(specification).Any();
        if (!stated && !inputs.Any(input => input.TargetProperty == destination.Target)) return null;
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
