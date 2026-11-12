// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Stage.Host.Extensions.Inbound;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// Keeps inbound requests in memory, deciding with the same rules the grains use, so a specification can run the endpoint and the dispatcher without a silo.
/// </summary>
/// <param name="time">The clock.</param>
/// <param name="options">The options.</param>
public sealed class InMemoryInboundDeliveries(TimeProvider time, InboundOptions options) : IInboundDeliveries
{
    readonly ConcurrentDictionary<string, InboundDelivery> _deliveries = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets every stored request.
    /// </summary>
    public IReadOnlyCollection<InboundDelivery> All => [.. _deliveries.Values];

    /// <inheritdoc/>
    public Task<InboundReceipt> Receive(InboundDelivery delivery)
    {
        _deliveries.TryGetValue(delivery.Id, out var existing);
        if (InboundDeliveryRules.IsReplay(existing, time.GetUtcNow(), options.ReplayWindow))
        {
            var counted = existing! with { Replays = existing.Replays + 1 };
            _deliveries[delivery.Id] = counted;
            return Task.FromResult(new InboundReceipt(counted, true));
        }

        var stored = delivery with { Status = InboundStatus.Pending, NextAttempt = delivery.Received };
        _deliveries[delivery.Id] = stored;
        return Task.FromResult(new InboundReceipt(stored, false));
    }

    /// <inheritdoc/>
    public Task<int> CountOpen() => Task.FromResult(_deliveries.Values.Count(delivery => delivery.IsOpen));

    /// <inheritdoc/>
    public Task<IReadOnlyList<InboundDelivery>> Due(DateTimeOffset now) =>
        Task.FromResult<IReadOnlyList<InboundDelivery>>([.. _deliveries.Values.Where(delivery => delivery.IsOpen && delivery.NextAttempt <= now).OrderBy(delivery => delivery.Received)]);

    /// <inheritdoc/>
    public Task<InboundDelivery> Settle(string id, InboundOutcome outcome, int responseStatus, string reason)
    {
        var current = _deliveries[id];
        var settled = current.IsOpen ? InboundDeliveryRules.Settle(current, outcome, responseStatus, reason, time.GetUtcNow(), options) : current;
        _deliveries[id] = settled;
        return Task.FromResult(settled);
    }

    /// <inheritdoc/>
    public Task<bool> Requeue(string id)
    {
        if (!_deliveries.TryGetValue(id, out var current) || current.Status is not (InboundStatus.Failed or InboundStatus.Rejected)) return Task.FromResult(false);
        _deliveries[id] = current with { Status = InboundStatus.Pending, Attempts = 0, NextAttempt = time.GetUtcNow(), Completed = null };
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<InboundDelivery?> Get(string id) => Task.FromResult(_deliveries.GetValueOrDefault(id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<InboundDelivery>> List(InboundStatus? status = null, int limit = 200) =>
        Task.FromResult<IReadOnlyList<InboundDelivery>>([.. _deliveries.Values.Where(delivery => status is null || delivery.Status == status).OrderByDescending(delivery => delivery.Received).Take(limit)]);
}
