// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;

/// <summary>
/// Reports one fixed owner process state.
/// </summary>
public class FakeOwnerProcesses : IOwnerProcesses
{
    /// <summary>
    /// Gets or sets the state every probe returns.
    /// </summary>
    public OwnerProcessState Result { get; set; } = OwnerProcessState.Unknown;

    /// <inheritdoc/>
    public OwnerProcessState State(int processId, long startedUnixSeconds) => Result;
}
