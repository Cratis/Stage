// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Runs a backend in a container of its own, from the image the deployment configures for the system.
/// </summary>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class ContainerBackendRuntime(IOptions<ExtensionOptions> options, ILogger<ContainerBackendRuntime> logger) : IBackendRuntime
{
    /// <summary>
    /// The port a backend listens on inside its container.
    /// </summary>
    public const int ContainerPort = 8080;

    /// <inheritdoc/>
    public ExecutionTier Tier => ExecutionTier.Container;

    /// <summary>
    /// Gets the name of the container of a launch, which is how it is stopped.
    /// </summary>
    /// <param name="launch">The <see cref="BackendLaunch"/>.</param>
    /// <returns>The container name.</returns>
    public static string NameOf(BackendLaunch launch) => $"stage-extension-{launch.System.ToLowerInvariant()}-{launch.Address.Port}";

    /// <summary>
    /// Gets the arguments of <c lang="text">run</c>.
    /// </summary>
    /// <param name="launch">The <see cref="BackendLaunch"/>.</param>
    /// <param name="container">The system's <see cref="ContainerOptions"/>.</param>
    /// <returns>The arguments, one by one, so no shell reads them.</returns>
    public static IReadOnlyList<string> RunArguments(BackendLaunch launch, ContainerOptions container)
    {
        var arguments = new List<string> { "run", "--rm", "--name", NameOf(launch), "--publish", $"{launch.Address.Host}:{launch.Address.Port}:{ContainerPort}" };
        foreach (var (name, value) in BackendFolders.Environment(launch, $"0.0.0.0:{ContainerPort}")) arguments.AddRange(["--env", $"{name}={value}"]);
        arguments.AddRange(container.Arguments);
        arguments.Add(container.Image);
        return arguments;
    }

    /// <inheritdoc/>
    public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
    {
        var execution = options.Value.Execution;
        if (!execution.Containers.TryGetValue(launch.System, out var container) || container.Image.Length == 0) throw new BackendStartException($"No container is configured for {launch.System}.");

        var info = new ProcessStartInfo(container.Command);
        foreach (var argument in RunArguments(launch, container)) info.ArgumentList.Add(argument);

        var name = NameOf(launch);
        var backend = ProcessBackend.Start(info, execution.StopTimeout, line => ActivationLog.BackendOutput(logger, launch.System, line), stopper: timeout => Stop(container, name, timeout));
        try
        {
            await BackendReadiness.WaitUntilListening(launch.Address, backend.Exited, execution.StartTimeout, cancellationToken);
            return backend;
        }
        catch
        {
            await backend.DisposeAsync();
            throw;
        }
    }

    static async Task Stop(ContainerOptions container, string name, TimeSpan timeout)
    {
        var info = new ProcessStartInfo(container.Command) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "stop", "--time", ((int)Math.Max(1, timeout.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture), name }) info.ArgumentList.Add(argument);
        using var stop = Process.Start(info);
        if (stop is not null) await stop.WaitForExitAsync();
    }
}
