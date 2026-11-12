// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation.given;

/// <summary>
/// A runtime whose backends are objects a specification controls.
/// </summary>
/// <param name="tier">The tier it starts backends for.</param>
public sealed class FakeBackendRuntime(ExecutionTier tier) : IBackendRuntime
{
    /// <summary>
    /// Gets what was started, in order.
    /// </summary>
    public List<BackendLaunch> Starts { get; } = [];

    /// <summary>
    /// Gets the backends that were started, in order.
    /// </summary>
    public List<ControlledBackend> Backends { get; } = [];

    /// <summary>
    /// Gets or sets why the next start fails, or null for it to succeed.
    /// </summary>
    public string? Failure { get; set; }

    /// <summary>
    /// Gets or sets a task the start waits for, which holds a start in progress.
    /// </summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <inheritdoc/>
    public ExecutionTier Tier => tier;

    /// <inheritdoc/>
    public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
    {
        Starts.Add(launch);
        if (Hold is not null) await Hold.Task;
        if (Failure is not null) throw new BackendStartException(Failure);

        var backend = new ControlledBackend(launch.System);
        Backends.Add(backend);
        return backend;
    }
}

/// <summary>
/// A backend a specification can crash.
/// </summary>
/// <param name="system">The system it serves.</param>
public sealed class ControlledBackend(string system) : StartedBackend
{
    readonly TaskCompletionSource<string> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the system the backend serves.
    /// </summary>
    public string System => system;

    /// <summary>
    /// Gets whether the backend was stopped by being disposed.
    /// </summary>
    public bool Stopped { get; private set; }

    /// <inheritdoc/>
    public override Task<string> Exited => _exited.Task;

    /// <summary>
    /// Makes the backend fail by itself.
    /// </summary>
    /// <param name="reason">Why.</param>
    public void Crash(string reason) => _exited.TrySetResult(reason);

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        Stopped = true;
        _exited.TrySetResult("stopped");
        return ValueTask.CompletedTask;
    }
}
