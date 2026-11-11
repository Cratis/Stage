// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// The credentials extensions present to the host services, and what each system's backend said it realizes. A credential is scoped to one system and
/// lives as long as the Stage process; it is handed to the backend at the handshake and nowhere else.
/// </summary>
public sealed class ExtensionSessions
{
    readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, ExtensionDescription> _descriptions = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the credential of a system, issuing it the first time.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The credential.</returns>
    public string TokenFor(string system) => _tokens.GetOrAdd(system, _ => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    /// <summary>
    /// Records what the backend of a system said at the handshake.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="description">The <see cref="ExtensionDescription"/>.</param>
    public void Connected(string system, ExtensionDescription description) => _descriptions[system] = description;

    /// <summary>
    /// Gets what the backend of a system said at the handshake.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The description, or null when no backend has connected for the system.</returns>
    public ExtensionDescription? Description(string system) => _descriptions.GetValueOrDefault(system);

    /// <summary>
    /// Finds the system a credential belongs to.
    /// </summary>
    /// <param name="token">The credential.</param>
    /// <returns>The name of the system, or null when the credential was not issued.</returns>
    public string? SystemOf(string token)
    {
        var presented = Encoding.UTF8.GetBytes(token);
        string? found = null;
        foreach (var (system, issued) in _tokens)
        {
            // Every credential is compared, so how long the search takes says nothing about which one matched.
            if (CryptographicOperations.FixedTimeEquals(presented, Encoding.UTF8.GetBytes(issued))) found = system;
        }

        return found;
    }
}
