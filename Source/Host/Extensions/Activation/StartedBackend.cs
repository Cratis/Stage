// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// A backend that runs. Stopping it is disposing it.
/// </summary>
public abstract class StartedBackend : IAsyncDisposable
{
    /// <summary>
    /// Gets a task that completes, with the reason, when the backend is gone for any reason, including having been stopped.
    /// </summary>
    public abstract Task<string> Exited { get; }

    /// <inheritdoc/>
    public abstract ValueTask DisposeAsync();
}
