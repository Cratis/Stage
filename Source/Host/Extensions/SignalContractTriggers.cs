// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Raises a trigger as a signal to every workflow that reacts to it. Each start is deduplicated on the trigger and its key, so a raise that is repeated, because an
/// extension retried or a webhook was delivered twice, finds the runs the first one started.
/// </summary>
/// <param name="routes">The <see cref="IContractTriggerRoutes"/>.</param>
/// <param name="workflows">The <see cref="IWorkflows"/>.</param>
public sealed class SignalContractTriggers(IContractTriggerRoutes routes, IWorkflows workflows) : IContractTriggers
{
    /// <inheritdoc/>
    public async Task<TriggerRaised> Signal(TriggerRaise raise)
    {
        if (raise.DeduplicationKey.Length == 0)
        {
            return new(TriggerRaiseStatus.Refused, "A trigger needs a deduplication key: without one it could be delivered twice.");
        }

        var reactions = routes.WorkflowsFor(raise.System, raise.Trigger);
        if (reactions.Count == 0)
        {
            return new(TriggerRaiseStatus.NotRouted, $"Nothing reacts to {raise.System}.{raise.Trigger}.");
        }

        var key = $"{raise.System}.{raise.Trigger}/{raise.DeduplicationKey}";
        foreach (var reaction in reactions)
        {
            var trigger = new WorkflowTrigger(
                WorkflowTriggerKind.Signal,
                new TriggerSourceKey($"{raise.System}.{raise.Trigger}"),
                new WorkflowDeduplicationKey($"{key}/{reaction}"),
                new WorkflowSubjectReference(new WorkflowSubjectKind("trigger"), new WorkflowSubjectId(key)),
                WorkflowContractReference.Any,
                new WorkflowData(raise.PayloadJson.Length == 0 ? "{}" : raise.PayloadJson),
                [],
                new WorkflowCorrelationKey(key),
                new WorkflowCausationKey(raise.Causation),
                WorkflowActor.System,
                DateTimeOffset.UtcNow);

            var admission = await workflows.Start(AbilityInvocations.Scope, new WorkflowId(reaction), trigger);
            if (!admission.Admitted)
            {
                return new(TriggerRaiseStatus.Refused, $"The engine did not admit {reaction}: {string.Join("; ", admission.Rejections.Select(rejection => rejection.Value))}");
            }
        }

        return new(TriggerRaiseStatus.Raised);
    }
}
