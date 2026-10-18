// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications;

// This loop belongs to Stage. Conformance must never use the reference reaction settlement here.
internal sealed class SemanticRunReactions(
    SemanticExecutionPlan plan,
    SemanticRunContext context,
    int historyCount,
    Func<SemanticSpecification, bool, Task<SemanticInvocationOutcome>> invoke)
{
    internal const int MaximumFacts = 1_000;
    readonly int _historyCount = historyCount;
    int _nextFact = historyCount;

    internal static SemanticRunReactionFailure FactLimit() =>
        SemanticRunReactionFailure.Unsupported($"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");

    internal async Task<SemanticRunReactionFailure?> Settle(CancellationToken cancellationToken)
    {
        while (_nextFact < context.Facts.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Facts.Count - _historyCount > MaximumFacts) return FactLimit();
            if (await React(_nextFact++, cancellationToken) is { } failure) return failure;
        }

        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Fire(SemanticSpecificationTrigger fired, CancellationToken cancellationToken)
    {
        var cause = new SemanticReactionCause(null, SemanticValue.Null, fired.Values.ToDictionary(value => value.TargetProperty, value => value.Value), context.AuthoredOccurrence);
        foreach (var reaction in plan.Reactions)
        {
            foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == fired.Kind && trigger.Source == fired.Trigger))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await Fire(reaction, trigger, cause, cancellationToken) is { } failure) return failure;
                if (await Settle(cancellationToken) is { } settlement) return settlement;
            }
        }

        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Advance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (!SemanticRunClock.TryFindDue(plan.Reactions, from, to, out var due))
        {
            return SemanticRunReactionFailure.Unsupported($"Advancing the clock makes more than {SemanticRunClock.MaximumOccurrences} occurrences due.");
        }
        foreach (var (at, reaction, trigger) in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await Fire(reaction, trigger, new(null, SemanticValue.Null, new Dictionary<SemanticId, SemanticValue>(), at), cancellationToken) is { } failure) return failure;
            if (await Settle(cancellationToken) is { } settlement) return settlement;
        }

        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Redeliver(SemanticId reaction, int givenIndex, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fact = context.Facts[givenIndex];
        var cause = new SemanticReactionCause(fact, context.Destinations[givenIndex], fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value), context.AuthoredOccurrence);
        foreach (var (selected, trigger) in Triggered(fact).Where(candidate => candidate.Reaction.Id == reaction))
        {
            if (await Fire(selected, trigger, cause, cancellationToken) is { } failure) return failure;
        }

        return await Settle(cancellationToken);
    }

    async Task<SemanticRunReactionFailure?> React(int index, CancellationToken cancellationToken)
    {
        var fact = context.Facts[index];
        var cause = new SemanticReactionCause(fact, context.Destinations[index], fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value), context.Occurrences[index]);
        foreach (var (reaction, trigger) in Triggered(fact))
        {
            if (await Fire(reaction, trigger, cause, cancellationToken) is { } failure) return failure;
        }

        return null;
    }

    IEnumerable<(SemanticReaction Reaction, SemanticReactionTrigger Trigger)> Triggered(SemanticSpecificationEvent fact)
    {
        var route = SemanticRunRoutes.Format(plan, fact.Route);

        return plan.Reactions.Where(reaction => MatchesFilter(reaction.From, route)).SelectMany(reaction => reaction.Triggers
            .Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == fact.EventContract)
            .Select(trigger => (reaction, trigger)));
    }

    bool MatchesFilter(SemanticObserverFilter? filter, SemanticEventRoute? route)
    {
        if (filter is null) return true;
        if (route is null) return false;
        var source = plan.Model.Application.EventSources.Single(source => source.Id == filter.Source);

        return source.SourceKind == route.SourceKind && (filter.Stream is null || source.Streams.Single(stream => stream.Id == filter.Stream.Value).StreamKind == route.StreamKind);
    }

    async Task<SemanticRunReactionFailure?> Fire(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticReactionCause cause, CancellationToken cancellationToken)
    {
        try
        {
            return await Effects(reaction, trigger, cause, cancellationToken);
        }
        catch (InvalidSemanticContract exception)
        {
            return SemanticRunReactionFailure.Reject(exception.Message);
        }
    }

    async Task<SemanticRunReactionFailure?> Effects(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticReactionCause cause, CancellationToken cancellationToken)
    {
        if (trigger.Where is not null && !SemanticRunReactionExpressions.Matches(trigger.Where, cause.Values)) return null;
        foreach (var produced in trigger.Produces)
        {
            if (await Produce(reaction, produced, cause, cancellationToken) is { } failure) return failure;
        }
        foreach (var invocation in trigger.Invokes)
        {
            var outcome = await invoke(Invocation(reaction, invocation, cause), !invocation.OnRefused.IsEmpty);
            if (outcome.Failure is null) continue;
            var branch = outcome.Refusal is { } refusal ? invocation.OnRefused.FirstOrDefault(refusal.Matches) : null;
            if (branch is null) return SemanticRunReactionFailure.Complete(outcome.Failure);
            foreach (var produced in branch.Produces)
            {
                if (await Produce(reaction, produced, cause, cancellationToken, outcome.Refusal) is { } failure) return failure;
            }
            break;
        }

        return null;
    }

    async Task<SemanticRunReactionFailure?> Produce(SemanticReaction reaction, SemanticProducedEvent produced, SemanticReactionCause cause, CancellationToken cancellationToken, SemanticInvocationRefusalOutcome? refusal = null)
    {
        if (cause.Occurred is null && produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))
        {
            return SemanticRunReactionFailure.Reject($"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.");
        }
        var (fact, destination) = Build(reaction, produced, cause, refusal);

        return await Append(fact, destination, cause.Occurred, cancellationToken);
    }

    (SemanticSpecificationEvent Fact, SemanticValue Destination) Build(SemanticReaction reaction, SemanticProducedEvent produced, SemanticReactionCause cause, SemanticInvocationRefusalOutcome? refusal)
    {
        if (cause.Fact is null && produced.Destination is null) throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must say with 'for' which event source it appends to, unless an event sets it off.");
        var contract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, SemanticRunReactionExpressions.Evaluate(mapping.Source, cause.Values, refusal, cause.Occurred))).ToArray();
        foreach (var value in mapped)
        {
            var property = contract.Properties.Single(property => property.Id == value.TargetProperty);
            SemanticRunReactionExpressions.Validate(plan, value.Value, property.Type, $"event property '{property.Name}'");
        }
        var destination = produced.Destination is null ? cause.Destination : SemanticRunReactionExpressions.Evaluate(produced.Destination, cause.Values);
        if (produced.Destination is not null) SemanticRunReactionExpressions.Validate(plan, destination, produced.DestinationType!, "reaction event source");

        return (new SemanticSpecificationEvent(contract.Id, [.. mapped])
        {
            EventSource = produced.Destination is null ? cause.Fact!.EventSource : new(produced.DestinationType!, destination)
        }, destination);
    }

    async Task<SemanticRunReactionFailure?> Append(SemanticSpecificationEvent fact, SemanticValue destination, DateTimeOffset? occurred, CancellationToken cancellationToken)
    {
        if (context.Facts.Count - _historyCount >= MaximumFacts) return FactLimit();
        var history = context.Facts.Select((item, index) => new SemanticConstraintEvaluator.Fact(item.EventContract, context.Destinations[index], item.Values)).ToArray();
        if (SemanticConstraintEvaluator.FindViolation(plan, history, [new(fact.EventContract, destination, fact.Values)]) is { } violation)
        {
            return SemanticRunReactionFailure.Reject(SemanticConstraintEvaluator.Message(violation), violation.Name);
        }
        if (SemanticRunAdmission.CheckProjectionFact(plan, fact) is { } blocker)
        {
            return SemanticRunReactionFailure.Refused(blocker);
        }
        await context.Append(fact, destination, occurred, cancellationToken);

        return null;
    }

    SemanticSpecification Invocation(SemanticReaction reaction, SemanticInvocation invocation, SemanticReactionCause cause)
    {
        var command = plan.Commands[invocation.Command];
        var mappings = invocation.Mappings.ToDictionary(mapping => mapping.TargetProperty);
        var inputs = command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(
            property.Id,
            mappings.TryGetValue(property.Id, out var mapping) ? SemanticRunReactionExpressions.Evaluate(mapping.Source, cause.Values, occurred: cause.Occurred) : SemanticValue.Null));

        return new(context.Specification.Id, context.Specification.Name, [], [], new(command.Id, [.. inputs]), [], [], [], [])
        {
            GivenClock = cause.Occurred?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            GivenCaller = reaction.RunsAs is { Kind: SemanticReactionIdentityKind.System, Roles.IsDefault: false } identity ? new(true, identity.Roles, []) : null
        };
    }
}

internal sealed record SemanticReactionCause(SemanticSpecificationEvent? Fact, SemanticValue Destination, IReadOnlyDictionary<SemanticId, SemanticValue> Values, DateTimeOffset? Occurred);

internal sealed record SemanticRunReactionFailure
{
    SemanticRunReactionFailure(SemanticRejection? rejection, string? unsupported, SemanticSpecificationRunRecord? result)
    {
        Rejection = rejection;
        UnsupportedMessage = unsupported;
        Result = result;
    }

    SemanticRejection? Rejection { get; }

    string? UnsupportedMessage { get; }

    SemanticSpecificationRunRecord? Result { get; }

    SemanticUnsupportedCapability? ProjectionBlocker { get; init; }

    internal static SemanticRunReactionFailure Reject(string message, string? code = null) => new(new(message, code), null, null);

    internal static SemanticRunReactionFailure Unsupported(string message) => new(null, message, null);

    internal static SemanticRunReactionFailure Refused(SemanticUnsupportedCapability blocker) => new(null, null, null) { ProjectionBlocker = blocker };

    internal static SemanticRunReactionFailure Complete(SemanticSpecificationRunRecord result) => new(null, null, result);

    internal SemanticSpecificationRunRecord ToRecord(SemanticSlice slice, SemanticSpecification specification) => ProjectionBlocker is { } blocker
        ? SemanticSpecificationExecutor.Unsupported(slice, specification, blocker) : Result ?? (UnsupportedMessage is { } message
        ? SemanticSpecificationExecutor.Unsupported(slice, specification, new(StageExecutionCapability.Reaction, specification.Id.ToString(), message))
        : SemanticSpecificationExecutor.Rejected(slice, specification, Rejection!));
}
