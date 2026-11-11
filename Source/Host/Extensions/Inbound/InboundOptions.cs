// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// How Stage stores and delivers the traffic that reaches an extension's endpoints, under <c lang="text">Stage:Extensions:Inbound</c>.
/// </summary>
public sealed class InboundOptions
{
    /// <summary>
    /// Gets or sets how many times a delivery is attempted before it is given up on and shown as failed.
    /// </summary>
    public int MaxAttempts { get; set; } = 8;

    /// <summary>
    /// Gets or sets the wait before the second attempt. Each following wait doubles, up to <see cref="MaxRetryDelay"/>.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the longest wait between two attempts.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets how long an identical request counts as a replay. Within the window a request with the same method, path, query and body as one already
    /// stored is acknowledged and not delivered again, which is also how a sender's own retries are absorbed.
    /// </summary>
    public TimeSpan ReplayWindow { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets how many deliveries may wait at once. Past it Stage answers 503 with a <c lang="text">Retry-After</c>, so an anonymous sender cannot fill storage.
    /// </summary>
    public int MaxOpen { get; set; } = 1000;

    /// <summary>
    /// Gets or sets how long the dispatcher sleeps when nothing is due.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets how long one attempt may take.
    /// </summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
