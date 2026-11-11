// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Finds what reacts to a trigger a system declares. The language support for reactions fills it in; nothing reacts until it does.
/// </summary>
public interface IContractTriggerRoutes
{
    /// <summary>
    /// Finds the workflows that start when a trigger is raised.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="trigger">The name of the trigger.</param>
    /// <returns>The ids of the workflows.</returns>
    IReadOnlyCollection<string> WorkflowsFor(string system, string trigger);
}

/// <summary>
/// Routes that react to nothing.
/// </summary>
public sealed class NoContractTriggerRoutes : IContractTriggerRoutes
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> WorkflowsFor(string system, string trigger) => [];
}
