// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// Where a stored inbound request is in its delivery to the extension.
/// </summary>
public enum InboundStatus
{
    /// <summary>
    /// Stored and acknowledged, not attempted yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// An attempt failed in a way that may pass, so another is scheduled.
    /// </summary>
    Retrying = 1,

    /// <summary>
    /// The extension accepted it.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// The extension refused it, because the request did not verify or is not one it handles. It is never retried, as the same request would be refused again.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Every attempt failed. It stays in the Workbench until someone retries it.
    /// </summary>
    Failed = 4
}
