// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// How long a request from the browser waits for the outcome before it answers that the invocation is pending.
/// </summary>
public sealed class BrowserInvocationOptions
{
    /// <summary>
    /// Gets or sets how long to wait for the outcome.
    /// </summary>
    public TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how often to look for the outcome while waiting.
    /// </summary>
    public TimeSpan Poll { get; set; } = TimeSpan.FromMilliseconds(100);
}
