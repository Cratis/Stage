// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// What one attempt to deliver a request amounts to.
/// </summary>
public enum InboundOutcome
{
    /// <summary>
    /// The extension accepted the request.
    /// </summary>
    Delivered,

    /// <summary>
    /// The extension refused the request for good.
    /// </summary>
    Rejected,

    /// <summary>
    /// The attempt failed in a way that may pass.
    /// </summary>
    Retry
}
