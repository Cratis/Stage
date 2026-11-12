// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// How an extension's backend runs, which decides what it costs while it is active and what it shares.
/// </summary>
public enum ExecutionTier
{
    /// <summary>
    /// No process: the extension is frontend-only or declarative.
    /// </summary>
    None,

    /// <summary>
    /// One host per runtime (<c lang="text">dotnet</c>, <c lang="text">node</c>, <c lang="text">jvm</c>) loads many extensions and isolates them within it.
    /// </summary>
    SharedHost,

    /// <summary>
    /// A small process of its own for the one extension.
    /// </summary>
    Native,

    /// <summary>
    /// Its own image and process, which is where an extension the deployment does not trust enough to share runs.
    /// </summary>
    Container
}

/// <summary>
/// Where a backend is in its life, which the Workbench shows.
/// </summary>
public enum BackendState
{
    /// <summary>
    /// Nothing runs. The next need starts it.
    /// </summary>
    Inactive,

    /// <summary>
    /// The backend is being started and is not yet accepting calls.
    /// </summary>
    Starting,

    /// <summary>
    /// The backend runs and accepts calls.
    /// </summary>
    Active,

    /// <summary>
    /// The backend is being stopped, after its idle timeout or when the host stops.
    /// </summary>
    Stopping
}
