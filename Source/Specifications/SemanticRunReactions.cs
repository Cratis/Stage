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
    readonly Dictionary<(SemanticId Command, SemanticId Property), Queue<SemanticValue>> _pinned = PinnedValues(context.Specification);
    readonly SemanticRunCaptures _captures = new(plan, context);
    readonly SemanticRunPublication _publication = new(plan, context, historyCount);
    readonly List<SemanticRecordedInvocation> _invocations = [];
    int _nextFact = historyCount;
    bool _capturesSeeded;

    // Every invocation the running reactions made, in order, whether or not the command accepted it or the slice owns it.
    internal IReadOnlyList<SemanticRecordedInvocation> Invocations => _invocations;

    internal static SemanticRunReactionFailure FactLimit() =>
        SemanticRunReactionFailure.Unsupported($"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");

    internal static SemanticRunReactionFailure? ContextGuard(SemanticReaction reaction, SemanticProducedEvent produced, DateTimeOffset? occurred) =>
        occurred is null && produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.EventSourceIdentity })
            ? SemanticRunReactionFailure.Reject($"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.")
            : null;

    internal async Task<SemanticRunReactionFailure?> Settle(CancellationToken cancellationToken)
    {
        if (!_capturesSeeded)
        {
            _capturesSeeded = true;
            if (_captures.Seed(_historyCount) is { } seedFailure) return seedFailure;
        }
        var delivered = new List<int>();
        while (_nextFact < context.Facts.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Facts.Count - _historyCount > MaximumFacts) return FactLimit();
            if (await _publication.PublishPending(cancellationToken) is { } publicationFailure) return publicationFailure;
            var index = _nextFact++;
            delivered.Add(index);
            if (await React(index, cancellationToken) is { } failure) return failure;
        }
        foreach (var index in delivered)
        {
            if (await _captures.Deliver(index, Settle, cancellationToken) is { } captureFailure) return captureFailure;
        }
        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Fire(SemanticSpecificationTrigger fired, CancellationToken cancellationToken)
    {
        var cause = new SemanticReactionCause(null, SemanticValue.Null, fired.Values.ToDictionary(value => value.TargetProperty, value => value.Value), context.AuthoredOccurrence);
        foreach (var reaction in plan.RunningReactions)
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
        if (!SemanticRunClock.TryFindDue(plan.RunningReactions, from, to, out var due))
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

        // Redelivery names its reaction explicitly, as the reference does.
        foreach (var (selected, trigger) in Triggered(plan.Reactions, fact).Where(candidate => candidate.Reaction.Id == reaction))
        {
            if (await Fire(selected, trigger, cause, cancellationToken) is { } failure) return failure;
        }

        return await Settle(cancellationToken);
    }

    // 'given generated <Command>.<property> = <value>' (ESM v30): each invocation by a reaction takes the next value pinned for its command and property.
    static Dictionary<(SemanticId Command, SemanticId Property), Queue<SemanticValue>> PinnedValues(SemanticSpecification specification)
    {
        var pinned = new Dictionary<(SemanticId Command, SemanticId Property), Queue<SemanticValue>>();
        foreach (var value in specification.GivenGenerated.IsDefault ? [] : specification.GivenGenerated)
        {
            if (!pinned.TryGetValue((value.Command, value.Property), out var queue)) pinned[(value.Command, value.Property)] = queue = new();
            queue.Enqueue(value.Value);
        }

        return pinned;
    }

    async Task<SemanticRunReactionFailure?> React(int index, CancellationToken cancellationToken)
    {
        var fact = context.Facts[index];
        var cause = new SemanticReactionCause(fact, context.Destinations[index], fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value), context.Occurrences[index]);
        foreach (var (reaction, trigger) in Triggered(plan.RunningReactions, fact))
        {
            if (await Fire(reaction, trigger, cause, cancellationToken) is { } failure) return failure;
        }

        return null;
    }

    IEnumerable<(SemanticReaction Reaction, SemanticReactionTrigger Trigger)> Triggered(IEnumerable<SemanticReaction> reactions, SemanticSpecificationEvent fact)
    {
        var route = SemanticRunRoutes.Format(plan, fact.Route);

        return reactions.Where(reaction => MatchesFilter(reaction.From, route)).SelectMany(reaction => reaction.Triggers
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
        if (await ProduceAll(reaction, trigger.Produces, cause, cancellationToken) is { } produceFailure) return produceFailure;
        foreach (var invocation in trigger.Invokes)
        {
            var invoked = Invocation(reaction, invocation, cause);
            var outcome = await invoke(invoked, !invocation.OnRefused.IsEmpty);
            _invocations.Add(new(invocation.Command, invoked.When!.Values, outcome.Failure is null));
            if (outcome.Failure is null)
            {
                if (await _publication.PublishPending(cancellationToken) is { } publicationFailure) return publicationFailure;
                continue;
            }
            var branch = outcome.Refusal is { } refusal ? invocation.OnRefused.FirstOrDefault(refusal.Matches) : null;
            if (branch is null) return SemanticRunReactionFailure.Complete(outcome.Failure);
            if (await ProduceAll(reaction, branch.Produces, cause, cancellationToken, outcome.Refusal) is { } branchFailure) return branchFailure;
            break;
        }

        return null;
    }

    // A batch is one trigger occurrence's direct productions, or one selected refusal branch's productions. Invoked commands stay separate units.
    async Task<SemanticRunReactionFailure?> ProduceAll(SemanticReaction reaction, IEnumerable<SemanticProducedEvent> productions, SemanticReactionCause cause, CancellationToken cancellationToken, SemanticInvocationRefusalOutcome? refusal = null)
    {
        var produced = productions.ToArray();
        if (plan.Model.Application.ReactionProductionMode is null)
        {
            foreach (var item in produced)
            {
                if (ContextGuard(reaction, item, cause.Occurred) is { } guard) return guard;
                var (fact, destination) = Build(reaction, item, cause, refusal);
                if (await Append(fact, destination, cause.Occurred, cancellationToken) is { } failure) return failure;
            }

            return null;
        }

        // Atomic: build every candidate, then validate the whole batch, and only then append. Nothing is appended before the last check passes.
        if (produced.Length == 0) return null;
        var candidates = new List<(SemanticSpecificationEvent Fact, SemanticValue Destination)>();
        foreach (var item in produced)
        {
            if (ContextGuard(reaction, item, cause.Occurred) is { } guard) return guard;
            candidates.Add(Build(reaction, item, cause, refusal));
        }
        if (context.Facts.Count - _historyCount + candidates.Count > MaximumFacts) return FactLimit();
        var claims = new SemanticReactionBatchClaims(plan, [.. context.Facts.Select((item, index) => new SemanticConstraintEvaluator.Fact(item.EventContract, context.Destinations[index], item.Values))]);
        foreach (var (fact, destination) in candidates)
        {
            if (claims.Validate(new(fact.EventContract, destination, fact.Values)) is { } violation)
            {
                return SemanticRunReactionFailure.Reject(SemanticConstraintEvaluator.Message(violation), violation.Name);
            }
        }
        foreach (var (fact, _) in candidates)
        {
            if (SemanticRunAdmission.CheckProjectionFact(plan, fact) is { } blocker) return SemanticRunReactionFailure.Refused(blocker);
        }
        foreach (var fact in candidates.Select(candidate => context.Prepare(candidate.Fact, candidate.Destination, cause.Occurred)).ToArray())
        {
            await context.Append(fact, cancellationToken);
        }

        return await _publication.PublishPending(cancellationToken);
    }

    (SemanticSpecificationEvent Fact, SemanticValue Destination) Build(SemanticReaction reaction, SemanticProducedEvent produced, SemanticReactionCause cause, SemanticInvocationRefusalOutcome? refusal)
    {
        if (cause.Fact is null && produced.Destination is null) throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must say with 'for' which event source it appends to, unless an event sets it off.");
        var contract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, SemanticRunReactionExpressions.Evaluate(mapping.Source, cause.Values, refusal, cause.Occurred, cause.Destination))).ToArray();
        foreach (var value in mapped)
        {
            var property = contract.Properties.Single(property => property.Id == value.TargetProperty);
            SemanticRunReactionExpressions.Validate(plan, value.Value, property.Type, $"event property '{property.Name}'");
        }
        var destination = produced.Destination is null ? cause.Destination : SemanticRunReactionExpressions.Evaluate(produced.Destination, cause.Values, eventSource: cause.Destination);
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

        return await _publication.PublishPending(cancellationToken);
    }

    SemanticSpecification Invocation(SemanticReaction reaction, SemanticInvocation invocation, SemanticReactionCause cause)
    {
        var command = plan.Commands[invocation.Command];
        var mappings = invocation.Mappings.ToDictionary(mapping => mapping.TargetProperty);
        var inputs = command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(
            property.Id,
            mappings.TryGetValue(property.Id, out var mapping) ? SemanticRunReactionExpressions.Evaluate(mapping.Source, cause.Values, occurred: cause.Occurred, eventSource: cause.Destination) : SemanticValue.Null));

        var generated = command.Properties.Where(property => property.IsGenerated && _pinned.TryGetValue((command.Id, property.Id), out var queue) && queue.Count > 0)
            .Select(property => new SemanticPropertyValue(property.Id, _pinned[(command.Id, property.Id)].Dequeue()));

        return new(context.Specification.Id, context.Specification.Name, [], [], new(command.Id, [.. inputs]) { GeneratedValues = [.. generated] }, [], [], [], [])
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
