// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Starts the backends of one <see cref="ExecutionTier"/>. A runtime returns once the backend accepts connections, so what a caller does next does not race the start.
/// </summary>
public interface IBackendRuntime
{
    /// <summary>
    /// Gets the tier this runtime starts backends for.
    /// </summary>
    ExecutionTier Tier { get; }

    /// <summary>
    /// Starts a backend.
    /// </summary>
    /// <param name="launch">What to start.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>The <see cref="StartedBackend"/>.</returns>
    /// <exception cref="BackendStartException">The backend did not start.</exception>
    Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken);
}
