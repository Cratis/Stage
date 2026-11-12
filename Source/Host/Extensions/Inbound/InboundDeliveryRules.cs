// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The decisions about stored inbound requests, kept apart from storage and transport so each can be held to a specification.
/// </summary>
public static class InboundDeliveryRules
{
    /// <summary>
    /// Gets the identity of a request: the system and a SHA-256 over the method, path, query and raw body. Headers are left out on purpose, because a sender's retry
    /// of the same message carries a fresh timestamp and signature; the extension, which verifies them, is what decides whether a request is genuine.
    /// </summary>
    /// <param name="system">The system.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path.</param>
    /// <param name="query">The raw query.</param>
    /// <param name="body">The raw body.</param>
    /// <returns>The identity.</returns>
    public static string Identify(string system, string method, string path, string query, ReadOnlySpan<byte> body)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendLine(hash, method.ToUpperInvariant());
        AppendLine(hash, path);
        AppendLine(hash, query);
        hash.AppendData(body);
        return $"{system}/{Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()}";
    }

    /// <summary>
    /// Decides whether a request that arrives is new or a replay of one already stored. A request is a replay while the stored one is still open, or arrived within
    /// the replay window and was not given up on: a sender that retries after every attempt failed is asking for the delivery to be made, so it is stored again.
    /// </summary>
    /// <param name="existing">The stored request with the same identity, or null.</param>
    /// <param name="now">The time.</param>
    /// <param name="window">The replay window.</param>
    /// <returns>True when the request is a replay and must not be delivered again.</returns>
    public static bool IsReplay(InboundDelivery? existing, DateTimeOffset now, TimeSpan window) =>
        existing is not null && (existing.IsOpen || (existing.Status != InboundStatus.Failed && now - existing.Received < window));

    /// <summary>
    /// Classifies the status an extension answered with.
    /// </summary>
    /// <param name="status">The HTTP status.</param>
    /// <returns>The <see cref="InboundOutcome"/>: success is delivered, a client error is a refusal that retrying would not change except for timeouts and throttling, and anything else may pass.</returns>
    public static InboundOutcome Classify(int status) => status switch
    {
        >= 200 and < 300 => InboundOutcome.Delivered,
        408 or 425 or 429 => InboundOutcome.Retry,
        >= 400 and < 500 => InboundOutcome.Rejected,
        _ => InboundOutcome.Retry
    };

    /// <summary>
    /// Gets how long to wait after an attempt that failed.
    /// </summary>
    /// <param name="attempts">How many attempts have been made.</param>
    /// <param name="options">The <see cref="InboundOptions"/>.</param>
    /// <returns>The wait: the base delay doubled for each earlier failure, up to the maximum.</returns>
    public static TimeSpan Backoff(int attempts, InboundOptions options)
    {
        var doublings = Math.Clamp(attempts - 1, 0, 30);
        var seconds = options.RetryDelay.TotalSeconds * Math.Pow(2, doublings);
        return TimeSpan.FromSeconds(Math.Min(seconds, options.MaxRetryDelay.TotalSeconds));
    }

    /// <summary>
    /// Records what an attempt ended with.
    /// </summary>
    /// <param name="delivery">The delivery.</param>
    /// <param name="outcome">What the attempt amounts to.</param>
    /// <param name="responseStatus">The status the extension answered with, or zero.</param>
    /// <param name="reason">What happened.</param>
    /// <param name="now">The time.</param>
    /// <param name="options">The <see cref="InboundOptions"/>.</param>
    /// <returns>The delivery as it is afterwards.</returns>
    public static InboundDelivery Settle(InboundDelivery delivery, InboundOutcome outcome, int responseStatus, string reason, DateTimeOffset now, InboundOptions options)
    {
        var attempts = delivery.Attempts + 1;
        var settled = delivery with { Attempts = attempts, ResponseStatus = responseStatus, Reason = reason };
        return outcome switch
        {
            InboundOutcome.Delivered => settled with { Status = InboundStatus.Delivered, Completed = now },
            InboundOutcome.Rejected => settled with { Status = InboundStatus.Rejected, Completed = now },
            _ when attempts >= options.MaxAttempts => settled with { Status = InboundStatus.Failed, Completed = now },
            _ => settled with { Status = InboundStatus.Retrying, NextAttempt = now + Backoff(attempts, options) }
        };
    }

    static void AppendLine(IncrementalHash hash, string value)
    {
        hash.AppendData(Encoding.UTF8.GetBytes(value));
        hash.AppendData("\n"u8);
    }
}
