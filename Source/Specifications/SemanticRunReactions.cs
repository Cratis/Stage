// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    internal static SemanticRunReactionFailure FactLimit() =>
        SemanticRunReactionFailure.Unsupported($"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");

    internal static SemanticRunReactionFailure? ContextGuard(SemanticReaction reaction, SemanticProducedEvent produced) =>
        produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression)
            ? SemanticRunReactionFailure.Reject($"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.")
            : null;

    internal async Task<SemanticRunReactionFailure?> Settle(CancellationToken cancellationToken)
    {
        for (var index = historyCount; index < context.Facts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Facts.Count - historyCount > MaximumFacts) return FactLimit();
            if (await React(index, cancellationToken) is { } failure) return failure;
        }

        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Redeliver(SemanticId reaction, int givenIndex, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fact = context.Facts[givenIndex];
        var cause = new SemanticReactionCause(fact, context.Destinations[givenIndex], fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value));
        foreach (var (selected, trigger) in Triggered(fact).Where(candidate => candidate.Reaction.Id == reaction))
        {
            if (await Fire(selected, trigger, cause, cancellationToken) is { } failure) return failure;
        }

        return await Settle(cancellationToken);
    }

    async Task<SemanticRunReactionFailure?> React(int index, CancellationToken cancellationToken)
    {
        var fact = context.Facts[index];
        var cause = new SemanticReactionCause(fact, context.Destinations[index], fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value));
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
        if (await ProduceAll(reaction, trigger.Produces, cause, cancellationToken) is { } produceFailure) return produceFailure;
        foreach (var invocation in trigger.Invokes)
        {
            var outcome = await invoke(Invocation(reaction, invocation, cause.Values), !invocation.OnRefused.IsEmpty);
            if (outcome.Failure is null) continue;
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
                if (ContextGuard(reaction, item) is { } guard) return guard;
                var (fact, destination) = Build(item, cause, refusal);
                if (await Append(fact, destination, cancellationToken) is { } failure) return failure;
            }

            return null;
        }

        // Atomic: build every candidate, then validate the whole batch, and only then append. Nothing is appended before the last check passes.
        if (produced.Length == 0) return null;
        var candidates = new List<(SemanticSpecificationEvent Fact, SemanticValue Destination)>();
        foreach (var item in produced)
        {
            if (ContextGuard(reaction, item) is { } guard) return guard;
            candidates.Add(Build(item, cause, refusal));
        }
        if (context.Facts.Count - historyCount + candidates.Count > MaximumFacts) return FactLimit();
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
        foreach (var fact in candidates.Select(candidate => context.Prepare(candidate.Fact, candidate.Destination)).ToArray())
        {
            await context.Append(fact, cancellationToken);
        }

        return null;
    }

    (SemanticSpecificationEvent Fact, SemanticValue Destination) Build(SemanticProducedEvent produced, SemanticReactionCause cause, SemanticInvocationRefusalOutcome? refusal)
    {
        var contract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, SemanticRunReactionExpressions.Evaluate(mapping.Source, cause.Values, refusal))).ToArray();
        foreach (var value in mapped)
        {
            var property = contract.Properties.Single(property => property.Id == value.TargetProperty);
            SemanticRunReactionExpressions.Validate(plan, value.Value, property.Type, $"event property '{property.Name}'");
        }
        var destination = produced.Destination is null ? cause.Destination : SemanticRunReactionExpressions.Evaluate(produced.Destination, cause.Values);
        if (produced.Destination is not null) SemanticRunReactionExpressions.Validate(plan, destination, produced.DestinationType!, "reaction event source");

        return (new SemanticSpecificationEvent(contract.Id, [.. mapped])
        {
            EventSource = produced.Destination is null ? cause.Fact.EventSource : new(produced.DestinationType!, destination)
        }, destination);
    }

    async Task<SemanticRunReactionFailure?> Append(SemanticSpecificationEvent fact, SemanticValue destination, CancellationToken cancellationToken)
    {
        if (context.Facts.Count - historyCount >= MaximumFacts) return FactLimit();
        var history = context.Facts.Select((item, index) => new SemanticConstraintEvaluator.Fact(item.EventContract, context.Destinations[index], item.Values)).ToArray();
        if (SemanticConstraintEvaluator.FindViolation(plan, history, [new(fact.EventContract, destination, fact.Values)]) is { } violation)
        {
            return SemanticRunReactionFailure.Reject(SemanticConstraintEvaluator.Message(violation), violation.Name);
        }
        if (SemanticRunAdmission.CheckProjectionFact(plan, fact) is { } blocker)
        {
            return SemanticRunReactionFailure.Refused(blocker);
        }
        await context.Append(fact, destination, cancellationToken);

        return null;
    }

    SemanticSpecification Invocation(SemanticReaction reaction, SemanticInvocation invocation, IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var command = plan.Commands[invocation.Command];
        var mappings = invocation.Mappings.ToDictionary(mapping => mapping.TargetProperty);
        var inputs = command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(
            property.Id,
            mappings.TryGetValue(property.Id, out var mapping) ? SemanticRunReactionExpressions.Evaluate(mapping.Source, values) : SemanticValue.Null));

        return new(context.Specification.Id, context.Specification.Name, [], [], new(command.Id, [.. inputs]), [], [], [], [])
        {
            GivenCaller = reaction.RunsAs is { Kind: SemanticReactionIdentityKind.System, Roles.IsDefault: false } identity ? new(true, identity.Roles, []) : null
        };
    }
}

internal sealed record SemanticReactionCause(SemanticSpecificationEvent Fact, SemanticValue Destination, IReadOnlyDictionary<SemanticId, SemanticValue> Values);

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
