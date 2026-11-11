// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// How a request to invoke from the browser ended.
/// </summary>
public enum BrowserInvocationStatus
{
    /// <summary>
    /// The screen places no such component.
    /// </summary>
    NotPlaced = 0,

    /// <summary>
    /// The contract does not let the component invoke the ability.
    /// </summary>
    NotAllowed = 1,

    /// <summary>
    /// The person is not authorized for the screen's component.
    /// </summary>
    Forbidden = 2,

    /// <summary>
    /// The ability path refused the invocation.
    /// </summary>
    Refused = 3,

    /// <summary>
    /// The invocation exists and has not settled yet. Asking again with the same interaction finds it.
    /// </summary>
    Pending = 4,

    /// <summary>
    /// The invocation settled.
    /// </summary>
    Settled = 5
}
