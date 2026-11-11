// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// What came of raising a trigger.
/// </summary>
public enum TriggerRaiseStatus
{
    /// <summary>
    /// Every reaction to the trigger was started, or already had been for the key.
    /// </summary>
    Raised = 0,

    /// <summary>
    /// Nothing reacts to the trigger.
    /// </summary>
    NotRouted = 1,

    /// <summary>
    /// The runtime did not take it.
    /// </summary>
    Refused = 2
}
