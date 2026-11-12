// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Starts backends when they are needed and stops them when they have been idle, the way Orleans manages grains. A backend that is not running costs nothing, and a call that finds
/// it not running waits for it to start, so the first call after a stop is slower and never lost.
/// </summary>
/// <param name="systems">The <see cref="ActivatedSystems"/>.</param>
/// <param name="runtimes">The <see cref="IBackendRuntime"/>s, one per tier.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class ExtensionActivator(ActivatedSystems systems, IEnumerable<IBackendRuntime> runtimes, IOptions<ExtensionOptions> options, TimeProvider time, ILogger<ExtensionActivator> logger) : IBackendActivity, IAsyncDisposable
{
    /// <summary>
    /// Occurs when a backend is no longer running, so whoever holds a connection to it lets go.
    /// </summary>
    public event Action<string>? Stopped;

    /// <summary>
    /// Gets the systems.
    /// </summary>
    public ActivatedSystems Systems => systems;

    /// <summary>
    /// Gets whether a system names an extension with a backend this host can start.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>True when the system has a backend.</returns>
    public bool HasBackend(string system) => systems.Find(system) is { Plan.Tier: not ExecutionTier.None };

    /// <summary>
    /// Makes sure the backend of a system runs, starting it when it does not and waiting until it accepts connections.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="reason">What caused it.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>The <see cref="ActivationOutcome"/>.</returns>
    public async Task<ActivationOutcome> Activate(string system, ActivationReason reason, CancellationToken cancellationToken)
    {
        if (systems.Find(system) is not { Plan.Tier: not ExecutionTier.None } slot) return new(null);

        await slot.Gate.WaitAsync(cancellationToken);
        try
        {
            if (slot.Backend is { } running)
            {
                if (!running.Exited.IsCompleted)
                {
                    slot.LastUsed = time.GetUtcNow();
                    return new(Endpoint(slot));
                }

                await Release(slot);
            }

            return await Start(slot, reason, cancellationToken);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    /// <summary>
    /// Starts the backend of a system without waiting for it, which is what reaching a screen does: the backend starts while the screen loads.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="reason">What caused it.</param>
    /// <returns>The state of the backend as it is now.</returns>
    public BackendState ActivateInBackground(string system, ActivationReason reason)
    {
        if (systems.Find(system) is not { Plan.Tier: not ExecutionTier.None } slot) return BackendState.Inactive;
        if (slot.State == BackendState.Active) return Touch(slot);

        if (slot.State == BackendState.Inactive) slot.State = BackendState.Starting;
        _ = Task.Run(async () =>
        {
            try
            {
                await Activate(system, reason, CancellationToken.None);
            }
            catch (Exception exception)
            {
                ActivationLog.ActivationFailed(logger, system, exception);
            }
        });
        return slot.State;
    }

    /// <inheritdoc/>
    public IDisposable Begin(string system)
    {
        if (systems.Find(system) is not { } slot) return NoActivity.Instance;

        slot.Enter();
        return new Activity(slot, time);
    }

    /// <summary>
    /// Stops the backends that have been idle for as long as their policy allows, and starts the ones whose policy keeps them running and are not.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the host stops.</param>
    /// <returns>A task that completes when the sweep is done.</returns>
    public async Task Sweep(CancellationToken cancellationToken)
    {
        foreach (var slot in systems.All)
        {
            if (slot.Plan.Tier == ExecutionTier.None) continue;
            if (slot.Policy.Always)
            {
                if (slot.Plan.CanRun && slot.Backend is null) await Activate(slot.System, ActivationReason.AlwaysOn, cancellationToken);
                continue;
            }

            if (slot.Backend is not null && IsIdle(slot)) await StopIdle(slot, cancellationToken);
        }
    }

    /// <summary>
    /// Describes every system, which is what the Workbench shows.
    /// </summary>
    /// <returns>The <see cref="ExtensionBackendStatus"/> of each system.</returns>
    public IReadOnlyList<ExtensionBackendStatus> Status() => [.. systems.All.OrderBy(slot => slot.System, StringComparer.Ordinal).Select(ExtensionBackendStatus.From)];

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var slot in systems.All)
        {
            await slot.Gate.WaitAsync();
            try
            {
                await Stop(slot, "the host is stopping");
            }
            finally
            {
                slot.Gate.Release();
            }
        }
    }

    static ExtensionEndpoint Endpoint(BackendSlot slot) => new(slot.System, slot.Address!, slot.Package.Digest);

    bool IsIdle(BackendSlot slot) => slot.InFlight == 0 && time.GetUtcNow() - slot.LastUsed >= slot.Policy.IdleTimeout;

    BackendState Touch(BackendSlot slot)
    {
        slot.LastUsed = time.GetUtcNow();
        return slot.State;
    }

    async Task<ActivationOutcome> Start(BackendSlot slot, ActivationReason reason, CancellationToken cancellationToken)
    {
        if (!slot.Plan.CanRun)
        {
            slot.Refusal = slot.Plan.Refusal;
            slot.State = BackendState.Inactive;
            return new(null, slot.Refusal);
        }

        var runtime = runtimes.FirstOrDefault(candidate => candidate.Tier == slot.Plan.Tier);
        if (runtime is null)
        {
            slot.Refusal = $"No runtime starts {slot.Plan.Tier} backends.";
            slot.State = BackendState.Inactive;
            return new(null, slot.Refusal);
        }

        slot.State = BackendState.Starting;
        ActivationLog.Activating(logger, slot.System, slot.Plan.Tier, reason);
        var execution = options.Value.Execution;
        var address = IPAddress.Parse(execution.ListenAddress);
        var uri = new Uri($"http://{execution.ListenAddress}:{BackendReadiness.FreePort(address)}");
        try
        {
            var backend = await runtime.Start(new(slot.System, slot.Package, slot.Plan, uri), cancellationToken);
            slot.Backend = backend;
            slot.Address = uri;
            slot.State = BackendState.Active;
            slot.Refusal = string.Empty;
            slot.LastActivation = time.GetUtcNow();
            slot.LastUsed = slot.LastActivation.Value;
            _ = Watch(slot, backend);
            ActivationLog.Activated(logger, slot.System, uri);
            return new(Endpoint(slot));
        }
        catch (BackendStartException exception)
        {
            slot.Refusal = exception.Message;
            slot.State = BackendState.Inactive;
            ActivationLog.StartFailed(logger, slot.System, exception.Message);
            return new(null, exception.Message);
        }
    }

    async Task Watch(BackendSlot slot, StartedBackend backend)
    {
        var reason = await backend.Exited;
        await slot.Gate.WaitAsync();
        try
        {
            if (!ReferenceEquals(slot.Backend, backend)) return;
            ActivationLog.Gone(logger, slot.System, reason);
            await Release(slot);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    async Task StopIdle(BackendSlot slot, CancellationToken cancellationToken)
    {
        await slot.Gate.WaitAsync(cancellationToken);
        try
        {
            if (slot.Backend is not null && IsIdle(slot)) await Stop(slot, "idle");
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    async Task Stop(BackendSlot slot, string reason)
    {
        if (slot.Backend is null) return;

        ActivationLog.Stopping(logger, slot.System, reason);
        slot.State = BackendState.Stopping;
        await Release(slot);
    }

    async Task Release(BackendSlot slot)
    {
        var backend = slot.Backend;
        slot.Backend = null;
        slot.Address = null;
        Stopped?.Invoke(slot.System);
        try
        {
            if (backend is not null) await backend.DisposeAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ActivationLog.StopFailed(logger, slot.System, exception);
        }

        slot.State = BackendState.Inactive;
        slot.LastStopped = time.GetUtcNow();
        slot.Refusal = string.Empty;
    }

    sealed class Activity(BackendSlot slot, TimeProvider time) : IDisposable
    {
        int _ended;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) != 0) return;
            slot.LastUsed = time.GetUtcNow();
            slot.Leave();
        }
    }

    sealed class NoActivity : IDisposable
    {
        public static readonly NoActivity Instance = new();

        public void Dispose()
        {
        }
    }
}
