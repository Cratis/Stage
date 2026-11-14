// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal static partial class SemanticWorldRebuilder
{
    internal static SemanticWorld Create(SemanticExecutionPlan plan, IEnumerable<AppendedEventResponse> events, ulong tail, bool exact = true, SemanticStorageEvidence? evidence = null) =>
        Create(new SemanticReplayPlan(plan), events, tail, exact, evidence);

    /// <summary>
    /// Rebuilds the world a stored history holds.
    /// </summary>
    /// <param name="replay">The execution plan and the body execution reducer transitions replay through, if any.</param>
    /// <param name="events">The stored events.</param>
    /// <param name="tail">The tail sequence number the history must end at.</param>
    /// <param name="exact">Whether the events were read back through storage; events the host holds in memory skip the storage checks.</param>
    /// <param name="evidence">What is known about how exactly storage kept decimals; omitted, none is.</param>
    /// <returns>The world.</returns>
    /// <exception cref="SemanticWorldRebuildRefused">The history cannot be read completely or is not a valid semantic world.</exception>
    internal static SemanticWorld Create(SemanticReplayPlan replay, IEnumerable<AppendedEventResponse> events, ulong tail, bool exact = true, SemanticStorageEvidence? evidence = null)
    {
        var (plan, bodies) = replay;
        var history = events.OrderBy(@event => @event.Context.SequenceNumber).ToArray();
        if (!IsCompleteThrough(history, tail))
        {
            throw new SemanticWorldRebuildRefused($"The event log cannot be read completely through tail {tail}.");
        }

        var ordinals = SeedOrdinals(history);
        evidence ??= SemanticStorageEvidence.None;
        var facts = history.Select(@event => Fact(plan, @event, exact, evidence with { Ordinal = ordinals.GetValueOrDefault(@event.Context.SequenceNumber) })).ToImmutableArray();
        SemanticRebuildConstraints.Check(plan, facts);
        try
        {
            return Outcome((bodies is null ? new SemanticEvaluator() : new SemanticEvaluator(bodies, bodies.Bodies)).EstablishWorld(SemanticProjectionAdmission.ForReplay(plan), facts));
        }
        catch (InvalidSemanticContract exception)
        {
            throw new SemanticWorldRebuildRefused($"The stored history is not a valid semantic world: {exception.Message}");
        }
    }

    internal static SemanticWorld Outcome(SemanticExecutionResult result) => result switch
    {
        SemanticAccepted accepted => accepted.World,
        SemanticUnsupported unsupported => throw new SemanticWorldRebuildRefused($"The semantic world cannot project its history: {unsupported.Details}"),
        SemanticRejected rejected => throw new SemanticWorldRebuildRefused($"The stored history is not a valid semantic world: {rejected.Details}"),
        _ => throw new SemanticWorldRebuildRefused("The semantic world reconstruction returned an unknown outcome.")
    };

    /// <summary>
    /// Reads a stored event as a semantic fact.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="stored">The stored event.</param>
    /// <param name="exact">
    /// Whether the event must be the exact storage form of a modeled production - what a restart verifies. Events the host has just appended
    /// itself, from memory, are not read back through storage, so they need not round-trip it (a decimal or a date time) nor name a producer.
    /// Stored seed events skip the production checks, identified by their causation subject, but are read back through storage like any other event.
    /// </param>
    /// <param name="evidence">What is known about how exactly storage kept decimals, and which model seed this event stands for; omitted, nothing is.</param>
    /// <returns>The fact.</returns>
    /// <exception cref="SemanticWorldRebuildRefused">The stored event is not one the model can have produced.</exception>
    internal static SemanticFact Fact(SemanticExecutionPlan plan, AppendedEventResponse stored, bool exact = true, SemanticStorageEvidence? evidence = null)
    {
        var context = stored.Context;
        var storageExact = exact;
        var isSeed = IsSeed(context);
        exact = exact && !isSeed;
        EnsureSupportedGeneration(stored);
        var contract = ResolveContract(plan, context);
        var route = Route(plan, context);
        evidence ??= SemanticStorageEvidence.None;
        var seedMustMatch = storageExact && EnsureExactStorageTypes(contract, plan.Model.Application, evidence.HasExactDecimals(context.EventType.Id), isSeed);

        var producers = Productions(plan, contract.Id).ToArray();
        if (exact)
        {
            EnsureNotPublishedPublicEvent(plan, contract, context, producers.Length);
        }

        var destinationType = DestinationType(plan, contract, route, exact);
        var destination = Text(context.EventSourceId, destinationType, plan.Model.Application);
        var occurred = OccurredOf(context);
        using var content = JsonDocument.Parse(stored.Content);
        var values = Values(content.RootElement, contract.Properties, plan.Model.Application);
        EnsureSeedMatches(seedMustMatch, evidence, contract, context, values);

        if (exact)
        {
            EnsureConsistentProductions(plan, new(producers, route, contract, values, context, occurred));
        }

        return new SemanticFact(contract.Id, destination, values)
        {
            Context = plan.Model.SemanticVersion != SemanticVersion.V1 ? new(new(destinationType, destination)) : null,
            Tags = [.. context.Tags],
            Occurred = occurred,
            CausedBy = CausedByOf(context),
            Route = route
        };
    }

    static bool IsCompleteThrough(AppendedEventResponse[] history, ulong tail) =>
        history.Length > 0 &&
        history[0].Context.SequenceNumber == 0 &&
        history[^1].Context.SequenceNumber == tail &&
        !history.Where((@event, index) => @event.Context.SequenceNumber != (ulong)index).Any();

    static DateTimeOffset OccurredOf(Chronicle.Contracts.Sequences.EventContext context) =>
        (DateTimeOffset?)context.Occurred is { } occurred
            ? occurred
            : throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has no occurrence time.");

    static SemanticCausedBy? CausedByOf(Chronicle.Contracts.Sequences.EventContext context) =>
        context.CausedBy is { } causedBy ? new SemanticCausedBy(causedBy.Subject ?? string.Empty, causedBy.Name ?? string.Empty, causedBy.UserName ?? string.Empty) : null;

    static void EnsureSupportedGeneration(AppendedEventResponse stored)
    {
        if (stored.Context.EventType.Generation != 1 || HasRevisionsOrGenerations(stored))
        {
            throw new SemanticWorldRebuildRefused($"Event {stored.Context.SequenceNumber} has an unsupported generation or revision.");
        }
    }

    static bool HasRevisionsOrGenerations(AppendedEventResponse stored) =>
        stored.Revisions.Any() || stored.GenerationalContent.Any(entry => entry.Key != 1);

    static SemanticEventContract ResolveContract(SemanticExecutionPlan plan, Chronicle.Contracts.Sequences.EventContext context)
    {
        var contracts = plan.Events.Values.Where(candidate => SemanticStoredEventNames.Of(candidate) == context.EventType.Id).ToArray();
        if (contracts.Length != 1)
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has unknown or ambiguous type '{context.EventType.Id}'.");
        }

        return contracts[0];
    }

    static bool IsSeed(Chronicle.Contracts.Sequences.EventContext context) =>
        context.CausedBy?.Subject == SemanticChronicleRegistration.SeedSubject;

    static void EnsureNotPublishedPublicEvent(SemanticExecutionPlan plan, SemanticEventContract contract, Chronicle.Contracts.Sequences.EventContext context, int producerCount)
    {
        // The reference evaluator cannot yet re-establish a history that holds published public events; refuse explicitly rather than guess.
        if (producerCount == 0 && IsPublishedPublicEvent(plan, contract))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} is a local publication of public event '{contract.Name}'; the semantic sandbox cannot yet rebuild a history with published events after a restart. Start a fresh session.");
        }
    }

    static bool IsPublishedPublicEvent(SemanticExecutionPlan plan, SemanticEventContract contract) =>
        plan.Projections.Values.Any(projection => projection.Target == SemanticProjectionTargetKind.Event && projection.ReadModel == contract.Id);

    static SemanticTypeReference DestinationType(SemanticExecutionPlan plan, SemanticEventContract contract, SemanticEventRoute? route, bool exact)
    {
        var routed = route is null ? null : plan.Model.Application.EventSources
            .Single(source => source.SourceKind == route.SourceKind).IdentifierType;
        if (routed is not null)
        {
            return routed;
        }

        // Screenplay falls back across all producers, not just those on the stored route.
        var destinations = DestinationTypes(plan, contract.Id, []).Distinct().ToArray();
        if (destinations is [{ IsOptional: false, IsCollection: false }])
        {
            return destinations[0]!;
        }

        if (exact)
        {
            throw new SemanticWorldRebuildRefused($"Event '{contract.Name}' has no unambiguous typed destination.");
        }

        // An event the host appended itself - seed data - need not be what a command produces; its source id is then plain text.
        return SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
    }

    static void EnsureConsistentProductions(SemanticExecutionPlan plan, ProductionCheck check)
    {
        var routed = check.Producers.Where(producer => RouteMatches(plan, producer.Route, check.Route)).ToArray();
        if (routed.Length == 0)
        {
            throw new SemanticWorldRebuildRefused($"Event {check.Context.SequenceNumber} has a route inconsistent with every modeled production of '{check.Contract.Name}'.");
        }

        if (!routed.Any(producer => ProductionMatches(producer.Produced, check)))
        {
            throw new SemanticWorldRebuildRefused($"Event {check.Context.SequenceNumber} has tags or occurrence values inconsistent with every modeled production of '{check.Contract.Name}'.");
        }
    }

    // Screenplay 4.141 settlement creates unrouted reaction productions, even from a routed cause.
    static IEnumerable<(SemanticProducedEvent Produced, SemanticCommandRoute? Route)> Productions(SemanticExecutionPlan plan, SemanticId eventContract) =>
        plan.Commands.Values.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => (produced, produced.Route ?? command.Route)))
            .Concat(plan.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(ReactionProductions)
                .Where(produced => produced.EventContract == eventContract)
                .Select(produced => (produced, (SemanticCommandRoute?)null)));

    static IEnumerable<SemanticTypeReference?> DestinationTypes(SemanticExecutionPlan plan, SemanticId eventContract, ImmutableHashSet<SemanticId> visited)
    {
        if (visited.Contains(eventContract))
        {
            return [];
        }

        var path = visited.Add(eventContract);
        var commands = plan.Commands.Values.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => produced.Destination is SemanticResolvedExpression resolved
                ? command.Properties.Single(property => property.Id == resolved.Target).Type
                : command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type));
        var reactions = plan.Reactions.SelectMany(reaction => reaction.Triggers)
            .Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event)
            .SelectMany(trigger => ReactionProductions(trigger).Where(produced => produced.EventContract == eventContract)
                .SelectMany(produced => produced.DestinationType is { } type
                    ? [type]
                    : DestinationTypes(plan, trigger.Source, path)));

        return commands.Concat(reactions);
    }

    static IEnumerable<SemanticProducedEvent> ReactionProductions(SemanticReactionTrigger trigger) =>
        trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces)));

    static bool ProductionMatches(SemanticProducedEvent produced, ProductionCheck check) =>
        check.Contract.Tags.AddRange(produced.Tags).SequenceEqual(check.Context.Tags) &&
        produced.Mappings
            .Where(mapping => mapping.Source is SemanticEventContextExpression)
            .All(mapping => ContextMappingMatches(mapping, check));

    static bool ContextMappingMatches(SemanticPropertyMapping mapping, ProductionCheck check)
    {
        var actual = check.Values.Single(value => value.TargetProperty == mapping.TargetProperty).Value;
        var expected = ((SemanticEventContextExpression)mapping.Source).Value switch
        {
            SemanticEventContextValueKind.Occurred => check.Occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            SemanticEventContextValueKind.CausedBySubject => check.Context.CausedBy?.Subject,
            SemanticEventContextValueKind.CausedByName => check.Context.CausedBy?.Name,
            SemanticEventContextValueKind.CausedByUserName => check.Context.CausedBy?.UserName,
            _ => null
        };

        return expected is not null && actual is SemanticTextValue text && text.Value == expected;
    }

    sealed record ProductionCheck(
        (SemanticProducedEvent Produced, SemanticCommandRoute? Route)[] Producers,
        SemanticEventRoute? Route,
        SemanticEventContract Contract,
        ImmutableArray<SemanticPropertyValue> Values,
        Chronicle.Contracts.Sequences.EventContext Context,
        DateTimeOffset Occurred);
}
