// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Starts what reacts to a trigger an extension raises.
/// </summary>
public interface IContractTriggers
{
    /// <summary>
    /// Signals a trigger.
    /// </summary>
    /// <param name="raise">The <see cref="TriggerRaise"/>.</param>
    /// <returns>The <see cref="TriggerRaised"/>.</returns>
    Task<TriggerRaised> Signal(TriggerRaise raise);
}
