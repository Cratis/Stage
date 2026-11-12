// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Marks backend calls as in flight, so a backend that is busy is not stopped for being idle.
/// </summary>
public interface IBackendActivity
{
    /// <summary>
    /// Marks a call to a backend as started.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>Disposed when the call ends.</returns>
    IDisposable Begin(string system);
}
