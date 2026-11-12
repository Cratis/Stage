// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Invocations;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// A backend Stage has completed the handshake with.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Client">The client of the backend.</param>
/// <param name="Description">What the backend said it realizes.</param>
public sealed record ExtensionConnection(string System, ExtensionBackend.ExtensionBackendClient Client, ExtensionDescription Description);

/// <summary>
/// The result of asking for a backend.
/// </summary>
/// <param name="Connection">The connection, or null when there is none.</param>
/// <param name="Refusal">Why there is none.</param>
public sealed record ExtensionConnectionResult(ExtensionConnection? Connection, string Refusal = "");

/// <summary>
/// Holds the backends Stage talks to: it finds one, does the handshake that checks it against its contract, and keeps the connection until it fails.
/// </summary>
/// <param name="endpoints">The <see cref="IExtensionEndpoints"/>.</param>
/// <param name="catalog">The <see cref="IAbilityCatalog"/>, which holds the contracts.</param>
/// <param name="sessions">The <see cref="ExtensionSessions"/>.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class ExtensionConnections(IExtensionEndpoints endpoints, IAbilityCatalog catalog, ExtensionSessions sessions, IOptions<ExtensionOptions> options, ILogger<ExtensionConnections> logger) : IDisposable
{
    readonly ConcurrentDictionary<string, (ExtensionConnection Connection, GrpcChannel Channel)> _connected = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the backend of a system, doing the handshake the first time. A handshake that fails is not remembered, so the next attempt tries again.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>The <see cref="ExtensionConnectionResult"/>.</returns>
    public async Task<ExtensionConnectionResult> Connect(string system, CancellationToken cancellationToken)
    {
        if (_connected.TryGetValue(system, out var existing)) return new(existing.Connection);

        var gate = _gates.GetOrAdd(system, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_connected.TryGetValue(system, out existing)) return new(existing.Connection);
            if (await endpoints.Resolve(system, cancellationToken) is not { } endpoint)
            {
                return new(null, $"No backend is known for the system {system}.");
            }

            var channel = GrpcChannel.ForAddress(endpoint.Address);
            var client = new ExtensionBackend.ExtensionBackendClient(channel);
            try
            {
                var response = await client.DescribeAsync(
                    new DescribeRequest
                    {
                        ProtocolVersion = new ProtocolVersion { Major = ExtensionOptions.ProtocolVersion.Major, Minor = ExtensionOptions.ProtocolVersion.Minor },
                        System = system,
                        PackageDigest = endpoint.PackageDigest,
                        HostEndpoint = options.Value.HostEndpoint,
                        SessionToken = sessions.TokenFor(system)
                    },
                    deadline: DateTime.UtcNow.Add(options.Value.DescribeTimeout),
                    cancellationToken: cancellationToken);

                if (response.ProtocolVersion?.Major != ExtensionOptions.ProtocolVersion.Major)
                {
                    channel.Dispose();
                    return new(null, $"The backend of {system} speaks protocol major {response.ProtocolVersion?.Major}; Stage speaks {ExtensionOptions.ProtocolVersion.Major}.");
                }

                var description = ExtensionDescription.From(response);
                var departures = ExtensionContract.Departures(catalog.All.Where(ability => ability.Reference.System == system), description);
                if (departures.Count > 0)
                {
                    channel.Dispose();
                    return new(null, $"The backend of {system} does not honor its contract: {string.Join(' ', departures)}");
                }

                var connection = new ExtensionConnection(system, client, description);
                sessions.Connected(system, description);
                _connected[system] = (connection, channel);
                ExtensionLog.Connected(logger, system, description.Name, description.Version);
                return new(connection);
            }
            catch (RpcException exception)
            {
                channel.Dispose();
                ExtensionLog.HandshakeFailed(logger, system, exception);
                return new(null, $"The handshake with the backend of {system} failed: {exception.Status.StatusCode} {exception.Status.Detail}");
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Gets the backend of a system when it is already connected, without starting it.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The connection, or null when the backend is not connected.</returns>
    public ExtensionConnection? Connected(string system) => _connected.TryGetValue(system, out var existing) ? existing.Connection : null;

    /// <summary>
    /// Drops the connection to a backend, so the next attempt does the handshake again. A backend that restarted needs it.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    public void Forget(string system)
    {
        if (_connected.TryRemove(system, out var removed)) removed.Channel.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var (_, channel) in _connected.Values) channel.Dispose();
        _connected.Clear();
    }
}
