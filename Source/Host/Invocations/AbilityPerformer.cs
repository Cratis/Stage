// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Stage.Contracts.Lifecycles;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Performs an ability for the workflow engine, as the node of an invocation run or as the action node of a workflow. What it does with an attempt
/// that may have reached the system depends on the <see cref="EffectClass"/>: <see cref="EffectClass.Safe"/> and <see cref="EffectClass.Idempotent"/>
/// attempts fail and are retried with the same idempotency key, and a <see cref="EffectClass.Once"/> attempt is never repeated and stops for reconciliation.
/// </summary>
/// <param name="descriptor">The ability.</param>
/// <param name="ledger">The <see cref="IInvocationLedger"/>.</param>
/// <param name="backend">The <see cref="IAbilityBackend"/> the extension protocol is behind.</param>
/// <param name="gates">The <see cref="SystemGates"/>.</param>
/// <param name="durability">The <see cref="InvocationDurability"/> of the ledger.</param>
/// <param name="logger">The logger.</param>
internal sealed class AbilityPerformer(AbilityDescriptor descriptor, IInvocationLedger ledger, IAbilityBackend backend, SystemGates gates, InvocationDurability durability, ILogger logger) : IWorkflowNodePerformer
{
    /// <inheritdoc/>
    public CapabilityReference Capability { get; } = new(descriptor.Reference.CapabilityId, descriptor.Reference.ContractMajor);

    /// <inheritdoc/>
    public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        if (durability.Refusal(descriptor) is { } refusal) return WorkflowNodeOutcome.Failed(refusal);
        var (origin, input, answerPolicy) = Describe(invocation);
        var record = await ledger.Record(descriptor, origin, answerPolicy);
        var grain = ledger.Of(record.Identity);
        var attempt = invocation.Attempt.Value;

        await gates.Pass(descriptor.Reference.System, cancellationToken);
        switch (await grain.BeginAttempt(attempt, invocation.Run.ToString(), invocation.Execution.ToString()))
        {
            case AttemptStart.Over:
                return await Recorded(grain);
            case AttemptStart.AlreadyBegun:
                var stopped = (await grain.Get())!.Reason;
                InvocationLog.NotRepeated(logger, record.Identity, descriptor.Reference.Capability, attempt);
                return WorkflowNodeOutcome.Ambiguous(stopped);
        }

        AbilityResult result;
        try
        {
            result = await backend.Invoke(new(descriptor.Reference, descriptor.Effect, record.Identity, attempt, input, descriptor.PackageDigest, descriptor.Timeout, AbilityInvocations.Scope.Value, origin), cancellationToken);
        }
        catch (Exception exception)
        {
            // An exception or a timeout says nothing about whether the call reached the system.
            result = new AbilityFailed(exception.Message);
        }

        return await Settle(grain, record.Identity, attempt, result);
    }

    static (InvocationOrigin Origin, string Input, string AnswerPolicy) Describe(WorkflowNodeInvocation invocation)
    {
        var node = JsonNode.Parse(invocation.Input.Value) as JsonObject ?? [];
        var payload = node["trigger"]?["payload"] as JsonObject;
        if (payload?["origin"] is JsonObject origin && Enum.TryParse<InvocationOriginKind>(origin["kind"]?.GetValue<string>(), out var kind) && origin["address"] is { } address && origin["causation"] is { } causation)
        {
            return (
                new(kind, address.GetValue<string>(), causation.GetValue<string>()),
                payload["input"]?.ToJsonString() ?? "{}",
                payload["answerPolicy"]?.GetValue<string>() ?? string.Empty);
        }

        // A workflow action performs the ability itself: its identity is the node execution of its run, and its input is composed from what the
        // model maps - literals, the subject the run started for and the fields earlier nodes returned.
        var configuration = JsonNode.Parse(invocation.Configuration.Value) as JsonObject;
        var address2 = configuration?["address"]?.GetValue<string>() ?? invocation.Execution.Node.Value;
        var input = configuration?["inputs"] is JsonArray inputs
            ? AbilityInputs.Compose(inputs, payload, node["nodes"] as JsonObject).ToJsonString()
            : invocation.Input.Value;
        return (
            new(InvocationOriginKind.WorkflowAction, address2, $"{invocation.Run}/{invocation.Execution}"),
            input,
            configuration?["answerPolicy"]?.GetValue<string>() ?? string.Empty);
    }

    static async Task<WorkflowNodeOutcome> Recorded(IInvocationGrain grain)
    {
        var record = (await grain.Get())!;
        return record.Status == InvocationStatus.Settled
            ? WorkflowNodeOutcome.Succeeded(record.Outcome, record.Output.Length == 0 ? null : new WorkflowData(record.Output))
            : WorkflowNodeOutcome.Failed(record.Reason.Length == 0 ? "The invocation already failed." : record.Reason);
    }

    async Task<WorkflowNodeOutcome> Settle(IInvocationGrain grain, string identity, int attempt, AbilityResult result)
    {
        switch (result)
        {
            case AbilityReturned returned:
                var outlet = returned.Outcome ?? AbilityDescriptor.ReturnedOutlet;
                if (!descriptor.Outlets.Contains(outlet))
                {
                    return await Unknown(grain, attempt, $"{descriptor.Reference.Capability} answered the outcome '{outlet}', which it does not declare.");
                }

                await grain.Settle(outlet, returned.Output, string.Empty);
                return WorkflowNodeOutcome.Succeeded(outlet, new WorkflowData(returned.Output));
            case AbilityAccepted when descriptor.AnswersLater:
                await grain.Suspended();
                return WorkflowNodeOutcome.Suspended(new WorkflowCompletionKey(identity), descriptor.AnswerExpiresAfter);
            case AbilityAccepted:
                return await Unknown(grain, attempt, $"{descriptor.Reference.Capability} accepted the work to answer later, which it does not declare.");
            case AbilityNotSent notSent:
                return await Ended(grain, attempt, notSent.Reason);
            case AbilityFailed { MayHaveReachedTheSystem: false } failed:
                return await Ended(grain, attempt, failed.Reason);
            case AbilityFailed failed:
                return await Unknown(grain, attempt, failed.Reason);
            default:
                return await Unknown(grain, attempt, $"{descriptor.Reference.Capability} answered something the runtime does not know.");
        }
    }

    // The attempt may have reached the system. Retrying is only right when doing it twice is.
    async Task<WorkflowNodeOutcome> Unknown(IInvocationGrain grain, int attempt, string reason)
    {
        if (descriptor.Effect != EffectClass.Once) return await Ended(grain, attempt, reason);
        await grain.Stopped(reason);
        return WorkflowNodeOutcome.Ambiguous(reason);
    }

    async Task<WorkflowNodeOutcome> Ended(IInvocationGrain grain, int attempt, string reason)
    {
        await grain.AttemptEnded(reason, attempt >= gates.Attempts(descriptor));
        return WorkflowNodeOutcome.Failed(reason);
    }
}
