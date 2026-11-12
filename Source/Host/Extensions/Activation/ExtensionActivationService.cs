// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Keeps backends in line with their policy while the host runs: starts the ones that must always run, stops the idle ones, and stops them all when the host stops.
/// </summary>
/// <param name="activator">The <see cref="ExtensionActivator"/>.</param>
/// <param name="connections">The <see cref="ExtensionConnections"/>, which let go of a backend that stopped, so the next call finds it and starts it again.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class ExtensionActivationService(ExtensionActivator activator, ExtensionConnections connections, IOptions<ExtensionOptions> options, TimeProvider time, ILogger<ExtensionActivationService> logger) : BackgroundService
{
    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await activator.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        activator.Stopped += connections.Forget;
        using var timer = new PeriodicTimer(options.Value.Execution.SweepInterval, time);
        do
        {
            try
            {
                await activator.Sweep(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                ActivationLog.SweepFailed(logger, exception);
            }
        }
        while (await WaitForNext(timer, stoppingToken));
    }

    static async Task<bool> WaitForNext(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
