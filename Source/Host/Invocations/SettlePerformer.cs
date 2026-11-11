// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The last node of an invocation run: whichever way the ability ended - it returned, an answer arrived, an operator reconciled it, its attempts ran
/// out - it records the outcome and takes it back to the origin.
/// </summary>
/// <param name="ledger">The <see cref="IInvocationLedger"/>.</param>
/// <param name="settlements">The <see cref="InvocationSettlements"/>.</param>
internal sealed class SettlePerformer(IInvocationLedger ledger, InvocationSettlements settlements) : IWorkflowNodePerformer
{
    /// <summary>
    /// Gets the id of the capability the node performs.
    /// </summary>
    public const string CapabilityId = "stage.invocation.settle";

    /// <inheritdoc/>
    public CapabilityReference Capability { get; } = new(CapabilityId, 1);

    /// <inheritdoc/>
    public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        var node = JsonNode.Parse(invocation.Input.Value) as JsonObject ?? [];
        var identity = node["trigger"]?["subject"]?["id"]?.GetValue<string>() ?? string.Empty;
        var outcome = (JsonNode.Parse(invocation.Configuration.Value) as JsonObject)?["outcome"]?.GetValue<string>() ?? AbilityDescriptor.FailedOutlet;
        var grain = ledger.Of(identity);
        if (await grain.Get() is null) return WorkflowNodeOutcome.Failed($"The invocation {identity} was never recorded.");

        // An answer that did not come in time ends the invocation as a failure of its own kind.
        if (outcome == AbilityDescriptor.ExpiredOutlet) await grain.AttemptEnded("No answer arrived before the invocation expired.", true);
        var record = await grain.Settle(outcome == AbilityDescriptor.ExpiredOutlet ? AbilityDescriptor.FailedOutlet : outcome, string.Empty, string.Empty);
        await settlements.Deliver(record);
        return WorkflowNodeOutcome.Succeeded(WellKnownOutlets.Next);
    }
}
