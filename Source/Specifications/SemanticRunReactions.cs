// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications;

// This loop belongs to Stage. Conformance must never use the reference reaction settlement here.
internal sealed class SemanticRunReactions(
    SemanticExecutionPlan plan,
    SemanticRunContext context,
    int historyCount,
    Func<SemanticSpecification, Task<SemanticSpecificationRunRecord?>> invoke)
{
    internal const int MaximumFacts = 1_000;

    internal static SemanticRunReactionFailure FactLimit() => new(
        $"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.", StageExecutionCapability.Reaction);

    internal async Task<SemanticRunReactionFailure?> Settle(CancellationToken cancellationToken)
    {
        for (var index = historyCount; index < context.Facts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Facts.Count - historyCount > MaximumFacts) return FactLimit();
            var fact = context.Facts[index];
            var values = fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
            var route = SemanticRunRoutes.Format(plan, fact.Route);
            foreach (var reaction in plan.Reactions.Where(reaction => MatchesFilter(reaction.From, route)))
            {
                foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == fact.EventContract))
                {
                    if (await Fire(reaction, trigger, fact, context.Destinations[index], values, cancellationToken) is { } failure) return failure;
                }
            }
        }

        return null;
    }

    bool MatchesFilter(SemanticObserverFilter? filter, SemanticEventRoute? route)
    {
        if (filter is null) return true;
        if (route is null) return false;
        var source = plan.Model.Application.EventSources.Single(source => source.Id == filter.Source);

        return source.SourceKind == route.SourceKind && (filter.Stream is null || source.Streams.Single(stream => stream.Id == filter.Stream.Value).StreamKind == route.StreamKind);
    }

    async Task<SemanticRunReactionFailure?> Fire(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticSpecificationEvent cause, SemanticValue destination, Dictionary<SemanticId, SemanticValue> values, CancellationToken cancellationToken)
    {
        try
        {
            if (trigger.Where is not null && !SemanticRunReactionExpressions.Matches(trigger.Where, values)) return null;
            foreach (var produced in trigger.Produces)
            {
                if (await Produce(reaction, produced, cause, destination, values, cancellationToken) is { } failure) return failure;
            }
            foreach (var invocation in trigger.Invokes)
            {
                var fixture = Invocation(reaction, invocation, values);
                if (await invoke(fixture) is { } result) return new(string.Empty, Result: result);
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new(exception.Message);
        }

        return null;
    }

    async Task<SemanticRunReactionFailure?> Produce(SemanticReaction reaction, SemanticProducedEvent produced, SemanticSpecificationEvent cause, SemanticValue causeDestination, Dictionary<SemanticId, SemanticValue> values, CancellationToken cancellationToken)
    {
        if (produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))
        {
            return new($"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.");
        }
        var contract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, SemanticRunReactionExpressions.Evaluate(mapping.Source, values))).ToArray();
        foreach (var value in mapped)
        {
            var property = contract.Properties.Single(property => property.Id == value.TargetProperty);
            SemanticRunReactionExpressions.Validate(plan, value.Value, property.Type, $"event property '{property.Name}'");
        }
        var destination = produced.Destination is null ? causeDestination : SemanticRunReactionExpressions.Evaluate(produced.Destination, values);
        if (produced.Destination is not null) SemanticRunReactionExpressions.Validate(plan, destination, produced.DestinationType!, "reaction event source");
        var fact = new SemanticSpecificationEvent(contract.Id, [.. mapped])
        {
            EventSource = produced.Destination is null ? cause.EventSource : new(produced.DestinationType!, destination)
        };
        if (context.Facts.Count - historyCount >= MaximumFacts) return FactLimit();
        var history = context.Facts.Select((item, index) => new SemanticConstraintEvaluator.Fact(item.EventContract, context.Destinations[index], item.Values)).ToArray();
        if (SemanticConstraintEvaluator.FindViolation(plan, history, [new(contract.Id, destination, fact.Values)]) is { } violation)
        {
            return new(SemanticConstraintEvaluator.Message(violation), Code: violation.Name);
        }
        await context.Append(fact, destination, cancellationToken);

        return null;
    }

    SemanticSpecification Invocation(SemanticReaction reaction, SemanticInvocation invocation, Dictionary<SemanticId, SemanticValue> values)
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

internal sealed record SemanticRunReactionFailure(string Message, StageExecutionCapability? Capability = null, string? Code = null, SemanticSpecificationRunRecord? Result = null)
{
    internal SemanticSpecificationRunRecord ToRecord(SemanticSlice slice, SemanticSpecification specification) => Result ?? (Capability is { } capability
        ? SemanticSpecificationExecutor.Record(slice, specification, SemanticSpecificationOutcome.Unsupported, unsupported: new(capability, specification.Id.ToString(), Message))
        : SemanticSpecificationExecutor.Rejected(slice, specification, Message, Code));
}
