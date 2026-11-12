// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The grain of one stored inbound request, keyed by <see cref="InboundDelivery.Id"/>. Its state is the durable copy of the request and of its delivery.
/// </summary>
[Alias("Cratis.Stage.Host.Extensions.Inbound.IInboundDeliveryGrain")]
public interface IInboundDeliveryGrain : IGrainWithStringKey
{
    /// <summary>
    /// Stores a request unless an identical one is already held. An identical request that is open, or that arrived within the replay window, is a replay and
    /// changes nothing; an older one that is over is replaced, because it can no longer be told from a new message.
    /// </summary>
    /// <param name="proposed">The request.</param>
    /// <param name="window">The replay window.</param>
    /// <returns>The delivery that holds the request, and whether it was a replay.</returns>
    [Alias("Receive")]
    Task<InboundReceipt> Receive(InboundDelivery proposed, TimeSpan window);

    /// <summary>
    /// Gets the delivery.
    /// </summary>
    /// <returns>The delivery, or null when nothing was stored.</returns>
    [Alias("Get")]
    Task<InboundDelivery?> Get();

    /// <summary>
    /// Records what an attempt ended with.
    /// </summary>
    /// <param name="outcome">What the attempt amounts to.</param>
    /// <param name="responseStatus">The status the extension answered with, or zero.</param>
    /// <param name="reason">What happened.</param>
    /// <returns>The delivery as it is afterwards.</returns>
    [Alias("Settle")]
    Task<InboundDelivery> Settle(InboundOutcome outcome, int responseStatus, string reason);

    /// <summary>
    /// Puts a delivery that failed or was rejected back for another round of attempts, on a person's decision.
    /// </summary>
    /// <returns>The delivery as it is afterwards, or null when nothing was stored or it is still open or already delivered.</returns>
    [Alias("Requeue")]
    Task<InboundDelivery?> Requeue();
}

/// <summary>
/// The result of storing a request.
/// </summary>
/// <param name="Delivery">The delivery that holds the request.</param>
/// <param name="Replay">Whether the request was a replay of one already stored.</param>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Extensions.Inbound.InboundReceipt")]
public sealed record InboundReceipt(
    [property: Id(0)] InboundDelivery Delivery,
    [property: Id(1)] bool Replay);
