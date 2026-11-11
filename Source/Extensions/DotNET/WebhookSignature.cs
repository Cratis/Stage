// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Extensions;

/// <summary>
/// Verifies the signature a webhook's sender puts on a request, so an extension raises a trigger only for a request that is genuine. Stage keeps the raw body and
/// hands it over unchanged on every attempt, which is what makes the signature verify; take the secret from <see cref="HostServices.Secret(string, CancellationToken)"/>, never from the
/// model or the code.
/// </summary>
/// <remarks>
/// The signature is an HMAC-SHA256 as lowercase hexadecimal, optionally prefixed with <c lang="text">sha256=</c>. Comparison takes the same time however many
/// characters match. A request without a valid signature must be answered with <c lang="text">401</c>: Stage then rejects it for good and does not retry it.
/// </remarks>
public static class WebhookSignature
{
    const string Prefix = "sha256=";

    /// <summary>
    /// Computes the signature of a payload.
    /// </summary>
    /// <param name="secret">The shared secret.</param>
    /// <param name="payload">What is signed.</param>
    /// <returns>The signature, as lowercase hexadecimal.</returns>
    public static string Compute(string secret, ReadOnlySpan<byte> payload) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload)).ToLowerInvariant();

    /// <summary>
    /// Verifies a signature computed over the body alone.
    /// </summary>
    /// <param name="secret">The shared secret. An empty secret verifies nothing.</param>
    /// <param name="body">The raw body.</param>
    /// <param name="signature">The signature the sender sent, or null when the header is missing.</param>
    /// <returns>True when the signature is valid.</returns>
    public static bool Verify(string secret, ReadOnlySpan<byte> body, string? signature) =>
        secret.Length > 0 && Matches(Compute(secret, body), signature);

    /// <summary>
    /// Verifies a signature computed over the timestamp and the body, written <c lang="text">&lt;timestamp&gt;.&lt;body&gt;</c>, and refuses a request whose timestamp is
    /// too far from now. This is what stops a captured request from being replayed later: the timestamp is signed, so it cannot be moved forward.
    /// </summary>
    /// <param name="secret">The shared secret. An empty secret verifies nothing.</param>
    /// <param name="body">The raw body.</param>
    /// <param name="timestamp">The timestamp the sender signed, in seconds since 1970.</param>
    /// <param name="signature">The signature the sender sent, or null when the header is missing.</param>
    /// <param name="now">The time now.</param>
    /// <param name="tolerance">How far the timestamp may be from now, in either direction.</param>
    /// <returns>True when the signature is valid and the timestamp is within the tolerance.</returns>
    public static bool Verify(string secret, ReadOnlySpan<byte> body, long timestamp, string? signature, DateTimeOffset now, TimeSpan tolerance)
    {
        if (secret.Length == 0 || tolerance < TimeSpan.Zero) return false;
        if (Math.Abs(now.ToUnixTimeSeconds() - timestamp) > tolerance.TotalSeconds) return false;

        var prefix = Encoding.UTF8.GetBytes($"{timestamp}.");
        var payload = new byte[prefix.Length + body.Length];
        prefix.CopyTo(payload, 0);
        body.CopyTo(payload.AsSpan(prefix.Length));
        return Matches(Compute(secret, payload), signature);
    }

    static bool Matches(string expected, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var candidate = signature.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? signature[Prefix.Length..] : signature;
        return candidate.Length == expected.Length && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate.ToLowerInvariant()), Encoding.ASCII.GetBytes(expected));
    }
}
