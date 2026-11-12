// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Runs backends in a shared host: one process per runtime that loads many extensions. The host starts with the first extension that needs it and stops with the last.
/// </summary>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class SharedHostBackendRuntime(IOptions<ExtensionOptions> options, ILogger<SharedHostBackendRuntime> logger) : IBackendRuntime, IDisposable
{
    readonly Dictionary<string, SharedHost> _hosts = [with(comparer: StringComparer.OrdinalIgnoreCase)];
    readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc/>
    public ExecutionTier Tier => ExecutionTier.SharedHost;

    /// <summary>
    /// Gets how many shared host processes run.
    /// </summary>
    public int Hosts
    {
        get
        {
            lock (_hosts) return _hosts.Count;
        }
    }

    /// <inheritdoc/>
    public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
    {
        var execution = options.Value.Execution;
        var runtime = launch.Plan.Runtime;
        if (!execution.Hosts.TryGetValue(runtime, out var configured) || configured.Command.Length == 0) throw new BackendStartException($"No shared host is configured for {runtime}.");

        var folder = BackendFolders.Unpack(launch, execution);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var host = Find(runtime) ?? Begin(runtime, configured);
            try
            {
                var exited = await host.Load(launch.System, launch.Address.Port, folder, execution.StartTimeout, cancellationToken);
                await BackendReadiness.WaitUntilListening(launch.Address, exited, execution.StartTimeout, cancellationToken);
                return new HostedBackend(this, host, runtime, launch.System, folder, exited);
            }
            catch
            {
                if (host.Loaded == 0) await End(runtime, host);
                throw;
            }
        }
        catch
        {
            BackendFolders.Remove(folder);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _gate.Dispose();

    SharedHost? Find(string runtime)
    {
        lock (_hosts) return _hosts.TryGetValue(runtime, out var host) && host.IsAlive ? host : null;
    }

    SharedHost Begin(string runtime, SharedHostOptions configured)
    {
        var host = SharedHost.Start(configured, logger);
        lock (_hosts) _hosts[runtime] = host;
        return host;
    }

    async Task End(string runtime, SharedHost host)
    {
        lock (_hosts)
        {
            if (_hosts.TryGetValue(runtime, out var current) && ReferenceEquals(current, host)) _hosts.Remove(runtime);
        }

        await host.Stop(options.Value.Execution.StopTimeout);
    }

    async Task Unload(string runtime, SharedHost host, string system, string folder)
    {
        await _gate.WaitAsync();
        try
        {
            await host.Unload(system, options.Value.Execution.StopTimeout);
            BackendFolders.Remove(folder);
            if (host.Loaded == 0) await End(runtime, host);
        }
        finally
        {
            _gate.Release();
        }
    }

    sealed class HostedBackend(SharedHostBackendRuntime owner, SharedHost host, string runtime, string system, string folder, Task<string> exited) : StartedBackend
    {
        int _disposed;

        public override Task<string> Exited => exited;

        public override async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) await owner.Unload(runtime, host, system, folder);
        }
    }
}
