// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Cratis.Showrunner.Notes;
using Cratis.Showrunner.Notes.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Registers the Notes artifacts in the event store of every namespace the application has, once each, and looks for new
/// namespaces at the Notes polling interval. See <see cref="NotesChronicleArtifacts"/> for why this is not the client's own registration.
/// </summary>
/// <param name="tenants">The event stores Notes works in, one per namespace.</param>
/// <param name="options">Notes' options, which set the polling interval.</param>
/// <param name="time">The clock the interval is measured on.</param>
/// <param name="artifacts">The provider the Notes artifacts are discovered from.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class NotesChronicleRegistration(
    TenantEventStores tenants,
    IOptions<NotesOptions> options,
    TimeProvider time,
    IClientArtifactsProvider artifacts,
    ILogger<NotesChronicleRegistration> logger) : BackgroundService
{
    readonly ConcurrentDictionary<IEventStore, bool> _registered = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Registers the Notes artifacts in every namespace that has not got them yet.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    internal async Task RunOnce(CancellationToken cancellationToken = default)
    {
        foreach (var store in await tenants.All(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_registered.ContainsKey(store))
            {
                continue;
            }

            await NotesChronicleArtifacts.Register(store, artifacts);
            _registered[store] = true;
            RegisteredArtifacts(store.Name.Value, store.Namespace.Value);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                RegistrationFailed(exception);
            }

            try
            {
                await Task.Delay(options.Value.PollingInterval, time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(LogLevel.Information, "Registered the Notes artifacts in event store '{EventStore}', namespace '{Namespace}'")]
    partial void RegisteredArtifacts(string eventStore, string @namespace);

    [LoggerMessage(LogLevel.Error, "Registering the Notes artifacts with Chronicle failed; it is tried again at the next round")]
    partial void RegistrationFailed(Exception exception);
}
