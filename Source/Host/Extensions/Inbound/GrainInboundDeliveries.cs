// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// Keeps inbound requests in grains, so they survive a restart and a request acknowledged is never lost.
/// </summary>
/// <param name="grains">The <see cref="IGrainFactory"/>.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
public sealed class GrainInboundDeliveries(IGrainFactory grains, Microsoft.Extensions.Options.IOptions<ExtensionOptions> options) : IInboundDeliveries
{
    IInboundIndexGrain Index => grains.GetGrain<IInboundIndexGrain>(IInboundIndexGrain.Key);

    /// <inheritdoc/>
    public async Task<InboundReceipt> Receive(InboundDelivery delivery)
    {
        var receipt = await grains.GetGrain<IInboundDeliveryGrain>(delivery.Id).Receive(delivery, options.Value.Inbound.ReplayWindow);
        if (!receipt.Replay) await Index.Open(delivery.Id);
        return receipt;
    }

    /// <inheritdoc/>
    public async Task<int> CountOpen() => (await Index.ListOpen()).Count;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InboundDelivery>> Due(DateTimeOffset now)
    {
        var open = new List<InboundDelivery>();
        foreach (var id in await Index.ListOpen())
        {
            if (await Get(id) is { IsOpen: true } delivery) open.Add(delivery);
            else await Index.Close(id);
        }

        return [.. open.Where(delivery => delivery.NextAttempt <= now).OrderBy(delivery => delivery.Received)];
    }

    /// <inheritdoc/>
    public async Task<InboundDelivery> Settle(string id, InboundOutcome outcome, int responseStatus, string reason)
    {
        var settled = await grains.GetGrain<IInboundDeliveryGrain>(id).Settle(outcome, responseStatus, reason);
        if (!settled.IsOpen) await Index.Close(id);
        return settled;
    }

    /// <inheritdoc/>
    public async Task<bool> Requeue(string id)
    {
        if (await grains.GetGrain<IInboundDeliveryGrain>(id).Requeue() is null) return false;
        await Index.Open(id);
        return true;
    }

    /// <inheritdoc/>
    public Task<InboundDelivery?> Get(string id) => grains.GetGrain<IInboundDeliveryGrain>(id).Get();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InboundDelivery>> List(InboundStatus? status = null, int limit = 200)
    {
        var listed = new List<InboundDelivery>();
        foreach (var id in await Index.Recent(InboundIndexGrain.Retained))
        {
            if (await Get(id) is not { } delivery || (status is not null && delivery.Status != status)) continue;
            listed.Add(delivery);
            if (listed.Count >= limit) break;
        }

        return listed;
    }
}
