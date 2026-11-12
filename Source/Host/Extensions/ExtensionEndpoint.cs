// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Finds where the backend of a system listens. Loading packages and activating their backends lazily is what fills it in; the default reads configuration.
/// </summary>
public interface IExtensionEndpoints
{
    /// <summary>
    /// Finds the backend of a system, activating it when it is not running.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>The <see cref="ExtensionEndpoint"/>, or null when no backend is known for the system.</returns>
    Task<ExtensionEndpoint?> Resolve(string system, CancellationToken cancellationToken);

    /// <summary>
    /// Tells whether a backend is known for a system, without activating it. An inbound request needs to know that much before it is stored, and a webhook should not wait for a cold start.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>True when a backend is known for the system.</returns>
    async Task<bool> Knows(string system, CancellationToken cancellationToken) => await Resolve(system, cancellationToken) is not null;
}

/// <summary>
/// Where the backend of a system listens.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Address">The address of the backend's gRPC endpoint.</param>
/// <param name="PackageDigest">The locked digest of the package the backend runs, or empty for a backend that runs by itself.</param>
public sealed record ExtensionEndpoint(string System, Uri Address, string PackageDigest = "");
