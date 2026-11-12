// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Packages;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The life of one system's backend: what it is, how it runs and where it is now.
/// </summary>
/// <param name="system">The name of the system.</param>
/// <param name="package">The package whose backend it is.</param>
/// <param name="plan">How it runs.</param>
/// <param name="policy">When it runs.</param>
public sealed class BackendSlot(string system, LoadedExtensionPackage package, ExecutionPlan plan, ActivationPolicy policy)
{
    int _inFlight;

    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public string System { get; } = system;

    /// <summary>
    /// Gets the package whose backend it is.
    /// </summary>
    public LoadedExtensionPackage Package { get; } = package;

    /// <summary>
    /// Gets how the backend runs.
    /// </summary>
    public ExecutionPlan Plan { get; } = plan;

    /// <summary>
    /// Gets when the backend runs.
    /// </summary>
    public ActivationPolicy Policy { get; } = policy;

    /// <summary>
    /// Gets where the backend is in its life.
    /// </summary>
    public BackendState State { get; internal set; }

    /// <summary>
    /// Gets the address of the running backend.
    /// </summary>
    public Uri? Address { get; internal set; }

    /// <summary>
    /// Gets when the backend was last started.
    /// </summary>
    public DateTimeOffset? LastActivation { get; internal set; }

    /// <summary>
    /// Gets when the backend was last stopped or found gone.
    /// </summary>
    public DateTimeOffset? LastStopped { get; internal set; }

    /// <summary>
    /// Gets when the backend was last needed.
    /// </summary>
    public DateTimeOffset LastUsed { get; internal set; }

    /// <summary>
    /// Gets why the backend could not start the last time it was tried, or empty.
    /// </summary>
    public string Refusal { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets how many calls are in flight.
    /// </summary>
    public int InFlight => Volatile.Read(ref _inFlight);

    /// <summary>
    /// Gets the running backend.
    /// </summary>
    internal StartedBackend? Backend { get; set; }

    /// <summary>
    /// Gets the lock that serializes starting and stopping.
    /// </summary>
    internal SemaphoreSlim Gate { get; } = new(1, 1);

    internal void Enter() => Interlocked.Increment(ref _inFlight);

    internal void Leave() => Interlocked.Decrement(ref _inFlight);
}
