// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Gives an ability invocation its identity. The identity is deterministic: the origin's semantic address plus its causation, and never the attempt.
/// It is the grain key of the invocation, the deduplication key of its run and the idempotency key the extension receives, so redelivery of a
/// reaction or a state machine entry finds the invocation that exists instead of starting another.
/// </summary>
public static class InvocationIdentity
{
    /// <summary>
    /// Gets the prefix of every identity.
    /// </summary>
    public const string Prefix = "inv-";

    /// <summary>
    /// Derives the identity of an invocation.
    /// </summary>
    /// <param name="origin">The <see cref="InvocationOrigin"/>.</param>
    /// <param name="ability">The ability invoked. Its contract major is not part of the identity, so an upgrade does not make a redelivery look new.</param>
    /// <returns>The identity.</returns>
    public static string For(InvocationOrigin origin, AbilityReference ability)
    {
        var text = string.Join('\0', (int)origin.Kind, origin.Address, origin.Causation, ability.CapabilityId);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Prefix + Convert.ToHexStringLower(hash.AsSpan(0, 16));
    }
}
