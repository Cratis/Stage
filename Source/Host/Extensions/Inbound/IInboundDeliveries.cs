// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The store of inbound requests: every request is kept here, with its raw body and headers, before the sender is answered.
/// </summary>
public interface IInboundDeliveries
{
    /// <summary>
    /// Stores a request. A replay of one already stored is recognised and not stored again.
    /// </summary>
    /// <param name="delivery">The request.</param>
    /// <returns>The <see cref="InboundReceipt"/>.</returns>
    Task<InboundReceipt> Receive(InboundDelivery delivery);

    /// <summary>
    /// Gets how many requests wait for an attempt.
    /// </summary>
    /// <returns>The count.</returns>
    Task<int> CountOpen();

    /// <summary>
    /// Gets the requests that are due an attempt.
    /// </summary>
    /// <param name="now">The time.</param>
    /// <returns>The deliveries, oldest first.</returns>
    Task<IReadOnlyList<InboundDelivery>> Due(DateTimeOffset now);

    /// <summary>
    /// Records what an attempt ended with.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <param name="outcome">What the attempt amounts to.</param>
    /// <param name="responseStatus">The status the extension answered with, or zero.</param>
    /// <param name="reason">What happened.</param>
    /// <returns>The delivery as it is afterwards.</returns>
    Task<InboundDelivery> Settle(string id, InboundOutcome outcome, int responseStatus, string reason);

    /// <summary>
    /// Puts a delivery that failed or was rejected back, on a person's decision.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <returns>True when it was put back.</returns>
    Task<bool> Requeue(string id);

    /// <summary>
    /// Gets a request.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <returns>The delivery, or null when it is unknown.</returns>
    Task<InboundDelivery?> Get(string id);

    /// <summary>
    /// Lists requests, newest first.
    /// </summary>
    /// <param name="status">The status to keep, or null for every status.</param>
    /// <param name="limit">The most to list.</param>
    /// <returns>The deliveries.</returns>
    Task<IReadOnlyList<InboundDelivery>> List(InboundStatus? status = null, int limit = 200);
}
