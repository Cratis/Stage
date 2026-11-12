// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// An inbound request Stage stored and acknowledged, and what became of delivering it to the extension. The raw body and headers are kept exactly as they
/// arrived, so the extension can verify a signature computed over them on every attempt.
/// </summary>
/// <param name="Id">The identity: the system and a fingerprint of the request, so an identical request finds this one.</param>
/// <param name="System">The system whose endpoint it reached.</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path below the system's mount, starting with a slash.</param>
/// <param name="Query">The raw query string without the question mark.</param>
/// <param name="Headers">The headers, without cookies and authorization.</param>
/// <param name="Body">The raw body.</param>
/// <param name="Received">When it arrived.</param>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Extensions.Inbound.InboundDelivery")]
public sealed record InboundDelivery(
    [property: Id(0)] string Id,
    [property: Id(1)] string System,
    [property: Id(2)] string Method,
    [property: Id(3)] string Path,
    [property: Id(4)] string Query,
    [property: Id(5)] InboundHeader[] Headers,
    [property: Id(6)] byte[] Body,
    [property: Id(7)] DateTimeOffset Received)
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    [Id(8)]
    public InboundStatus Status { get; init; } = InboundStatus.Pending;

    /// <summary>
    /// Gets how many attempts have been made.
    /// </summary>
    [Id(9)]
    public int Attempts { get; init; }

    /// <summary>
    /// Gets when the next attempt is due. Meaningful while the delivery is open.
    /// </summary>
    [Id(10)]
    public DateTimeOffset NextAttempt { get; init; }

    /// <summary>
    /// Gets what the last attempt ended with: the extension's status, or why it could not be reached.
    /// </summary>
    [Id(11)]
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status the extension answered with on the last attempt, or zero when it did not answer.
    /// </summary>
    [Id(12)]
    public int ResponseStatus { get; init; }

    /// <summary>
    /// Gets when the delivery reached a final status.
    /// </summary>
    [Id(13)]
    public DateTimeOffset? Completed { get; init; }

    /// <summary>
    /// Gets how many identical requests were absorbed as replays while this one was stored.
    /// </summary>
    [Id(14)]
    public int Replays { get; init; }

    /// <summary>
    /// Gets whether the delivery still waits for an attempt.
    /// </summary>
    public bool IsOpen => Status is InboundStatus.Pending or InboundStatus.Retrying;
}
