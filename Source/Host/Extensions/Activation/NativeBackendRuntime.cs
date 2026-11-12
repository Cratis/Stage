// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Runs a backend as a small process of its own: the package's entry point, unpacked and executed.
/// </summary>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class NativeBackendRuntime(IOptions<ExtensionOptions> options, ILogger<NativeBackendRuntime> logger) : IBackendRuntime
{
    /// <inheritdoc/>
    public ExecutionTier Tier => ExecutionTier.Native;

    /// <inheritdoc/>
    public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
    {
        var execution = options.Value.Execution;
        var folder = BackendFolders.Unpack(launch, execution);
        ProcessBackend? backend = null;
        try
        {
            var entry = BackendFolders.EntryPoint(launch, folder);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(entry, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            var info = new ProcessStartInfo(entry) { WorkingDirectory = folder };
            foreach (var (name, value) in BackendFolders.Environment(launch, $"{launch.Address.Host}:{launch.Address.Port}")) info.Environment[name] = value;

            backend = ProcessBackend.Start(info, execution.StopTimeout, line => ActivationLog.BackendOutput(logger, launch.System, line), cleanup: () => BackendFolders.Remove(folder));
            await BackendReadiness.WaitUntilListening(launch.Address, backend.Exited, execution.StartTimeout, cancellationToken);
            return backend;
        }
        catch
        {
            if (backend is not null) await backend.DisposeAsync();
            else BackendFolders.Remove(folder);
            throw;
        }
    }
}
