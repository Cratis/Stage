// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticRunReactionAdmission
{
    const int MaximumEvents = 1_000;

    internal static SemanticUnsupportedCapability? CheckProjections(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> initial)
    {
        var (reachable, _, blocker) = Reachable(plan, specification, initial);

        return blocker ?? SemanticRunProjectionAdmission.Check(plan, specification, reachable);
    }

    internal static SemanticUnsupportedCapability? CheckCommands(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        // Expected rejections must not hide an unsupported invocation behind a passing error assertion.
        var (_, commands, blocker) = Reachable(plan, specification, [], includeRejected: true);
        if (blocker is not null) return blocker;
        var feature = commands.SelectMany(id => SemanticVersionFeatures.UnsupportedReadRequirements(plan.Commands[id])).FirstOrDefault();

        return feature is null ? null : new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
    }

    internal static (HashSet<SemanticId> Events, HashSet<SemanticId> Commands, SemanticUnsupportedCapability? Blocker) Reachable(
        SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> initial, bool includeRejected = false)
    {
        var reachable = initial.ToHashSet();
        var commands = ActionTriggers(plan, specification).SelectMany(trigger => trigger.Invokes)
            .Select(invocation => invocation.Command).Where(plan.Commands.ContainsKey).ToHashSet();
        var pending = new Queue<SemanticId>(ActionEvents(plan, specification, includeRejected));
        var visited = new HashSet<SemanticId>();
        while (pending.TryDequeue(out var cause))
        {
            if (!visited.Add(cause)) continue;
            if (visited.Count > MaximumEvents)
            {
                return (reachable, commands, new(StageExecutionCapability.Reaction, specification.Id.ToString(), "Reaction projection reachability exceeds 1000 event contracts."));
            }
            reachable.Add(cause);
            foreach (var trigger in Triggers(plan, cause))
            {
                commands.UnionWith(trigger.Invokes.Select(invocation => invocation.Command).Where(plan.Commands.ContainsKey));
                foreach (var product in Effects(plan, trigger)) pending.Enqueue(product);
            }
        }

        return (reachable, commands, null);
    }

    static IEnumerable<SemanticId> ActionEvents(SemanticExecutionPlan plan, SemanticSpecification specification, bool includeRejected)
    {
        if (specification.WhenRedelivered is not null || specification.WhenTrigger is not null || specification.WhenClock is not null)
        {
            return ActionTriggers(plan, specification).SelectMany(trigger => Effects(plan, trigger));
        }
        if (specification.WhenAppended is { } appended) return [appended.EventContract];

        return specification.When is { } when && (includeRejected || (specification.ThenErrors.IsEmpty && !specification.ThenDenied)) && plan.Commands.TryGetValue(when.Command, out var command)
            ? command.Produces.Select(produced => produced.EventContract) : [];
    }

    static IEnumerable<SemanticReactionTrigger> ActionTriggers(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        if (specification.WhenTrigger is { } fired)
        {
            return plan.Reactions.SelectMany(reaction => reaction.Triggers).Where(trigger => trigger.Kind == fired.Kind && trigger.Source == fired.Trigger);
        }
        if (specification.WhenClock is not null)
        {
            // Admission is conservative: guards and clock windows must not hide unsupported effects.
            return plan.Reactions.SelectMany(reaction => reaction.Triggers).Where(trigger => trigger.Kind is SemanticReactionTriggerKind.Interval or SemanticReactionTriggerKind.Schedule);
        }

        return RedeliveryTriggers(plan, specification);
    }

    static IEnumerable<SemanticReactionTrigger> RedeliveryTriggers(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        if (specification.WhenRedelivered is not { } redelivery) return [];
        var given = specification.GivenEvents[redelivery.Given];

        return plan.Reactions.Where(reaction => reaction.Id == redelivery.Reaction && MatchesFilter(plan, reaction.From, given.Route))
            .SelectMany(reaction => reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == given.EventContract));
    }

    static IEnumerable<SemanticReactionTrigger> Triggers(SemanticExecutionPlan plan, SemanticId cause) =>
        plan.Reactions.SelectMany(reaction => reaction.Triggers)
            .Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == cause);

    static IEnumerable<SemanticId> Effects(SemanticExecutionPlan plan, SemanticReactionTrigger trigger) =>
        trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces)))
            .Select(produced => produced.EventContract)
            .Concat(trigger.Invokes.Where(invocation => plan.Commands.ContainsKey(invocation.Command))
                .SelectMany(invocation => plan.Commands[invocation.Command].Produces.Select(produced => produced.EventContract)));

    static bool MatchesFilter(SemanticExecutionPlan plan, SemanticObserverFilter? filter, SemanticFixtureRoute? route)
    {
        if (filter is null) return true;
        if (route is null) return false;
        var source = plan.Model.Application.EventSources.Single(source => source.Id == filter.Source);
        var routed = plan.Model.Application.EventSources.Single(source => source.Id == route.Source);

        return source.SourceKind == routed.SourceKind &&
            (filter.Stream is null || source.Streams.Single(stream => stream.Id == filter.Stream).StreamKind == routed.Streams.Single(stream => stream.Id == route.Stream).StreamKind);
    }
}
