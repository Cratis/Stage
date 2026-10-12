// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Semantics;

internal static class SemanticRunReactionAdmission
{
    const int MaximumEvents = 1_000;

    internal static SemanticUnsupportedCapability? CheckProjections(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> initial)
    {
        var (reachable, blocker) = Reachable(plan, specification, initial);

        return blocker ?? SemanticRunProjectionAdmission.Check(plan, specification, reachable);
    }

    static (HashSet<SemanticId> Events, SemanticUnsupportedCapability? Blocker) Reachable(
        SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> initial)
    {
        var reachable = initial.ToHashSet();
        var pending = new Queue<SemanticId>(ActionEvents(plan, specification));
        var visited = new HashSet<SemanticId>();
        while (pending.TryDequeue(out var cause))
        {
            if (!visited.Add(cause)) continue;
            if (visited.Count > MaximumEvents)
            {
                return (reachable, new(StageExecutionCapability.Reaction, specification.Id.ToString(), "Reaction projection reachability exceeds 1000 event contracts."));
            }
            reachable.Add(cause);
            foreach (var product in Products(plan, cause)) pending.Enqueue(product);
        }

        return (reachable, null);
    }

    static IEnumerable<SemanticId> ActionEvents(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        if (specification.WhenAppended is { } appended) return [appended.EventContract];

        return specification.When is { } when && specification.ThenErrors.IsEmpty && !specification.ThenDenied && plan.Commands.TryGetValue(when.Command, out var command)
            ? command.Produces.Select(produced => produced.EventContract) : [];
    }

    static IEnumerable<SemanticId> Products(SemanticExecutionPlan plan, SemanticId cause) =>
        plan.Reactions.SelectMany(reaction => reaction.Triggers)
            .Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == cause)
            .SelectMany(trigger => trigger.Produces.Select(produced => produced.EventContract)
                .Concat(trigger.Invokes.Where(invocation => plan.Commands.ContainsKey(invocation.Command))
                    .SelectMany(invocation => plan.Commands[invocation.Command].Produces.Select(produced => produced.EventContract))));
}
