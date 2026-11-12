// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The grain of one stored inbound request.
/// </summary>
/// <param name="state">The persisted state.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="time">The clock.</param>
public sealed class InboundDeliveryGrain(
    [PersistentState("inbound-delivery", WellKnownGrainStorageProviders.Workflows)] IPersistentState<InboundDeliveryState> state,
    IOptions<ExtensionOptions> options,
    TimeProvider time) : Grain, IInboundDeliveryGrain
{
    /// <inheritdoc/>
    public async Task<InboundReceipt> Receive(InboundDelivery proposed, TimeSpan window)
    {
        if (state.State.Delivery is { } existing && InboundDeliveryRules.IsReplay(existing, time.GetUtcNow(), window))
        {
            var counted = existing with { Replays = existing.Replays + 1 };
            state.State.Delivery = counted;
            await state.WriteStateAsync();
            return new(counted, true);
        }

        var stored = proposed with { Status = InboundStatus.Pending, NextAttempt = proposed.Received };
        state.State.Delivery = stored;
        await state.WriteStateAsync();
        return new(stored, false);
    }

    /// <inheritdoc/>
    public Task<InboundDelivery?> Get() => Task.FromResult(state.State.Delivery);

    /// <inheritdoc/>
    public async Task<InboundDelivery> Settle(InboundOutcome outcome, int responseStatus, string reason)
    {
        var current = state.State.Delivery ?? throw new InvalidOperationException($"The inbound request {this.GetPrimaryKeyString()} was never stored.");
        if (!current.IsOpen) return current;

        var settled = InboundDeliveryRules.Settle(current, outcome, responseStatus, reason, time.GetUtcNow(), options.Value.Inbound);
        state.State.Delivery = settled;
        await state.WriteStateAsync();
        return settled;
    }

    /// <inheritdoc/>
    public async Task<InboundDelivery?> Requeue()
    {
        if (state.State.Delivery is not { } current || current.Status is not (InboundStatus.Failed or InboundStatus.Rejected)) return null;

        var requeued = current with { Status = InboundStatus.Pending, Attempts = 0, NextAttempt = time.GetUtcNow(), Completed = null, Reason = "Retried by an operator." };
        state.State.Delivery = requeued;
        await state.WriteStateAsync();
        return requeued;
    }
}

/// <summary>
/// The persisted state of an <see cref="InboundDeliveryGrain"/>.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Extensions.Inbound.InboundDeliveryState")]
public sealed class InboundDeliveryState
{
    /// <summary>
    /// Gets or sets the delivery.
    /// </summary>
    [Id(0)]
    public InboundDelivery? Delivery { get; set; }
}
