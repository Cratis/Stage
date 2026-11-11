// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Holds back calls to a system that exceed its rate, from deployment configuration. Concurrency is the workflow engine's own capability gate, which
/// <see cref="Concurrency"/> configures from the same options.
/// </summary>
/// <param name="options">The invocation options.</param>
[IgnoreConvention]
public sealed class SystemGates(IOptions<StageInvocationOptions> options) : IDisposable
{
    readonly ConcurrentDictionary<string, RateLimiter?> _limiters = new(StringComparer.Ordinal);

    /// <summary>
    /// Maps the concurrency limits of the systems onto the workflow engine's capability limits: one gate per system, shared by every ability.
    /// </summary>
    /// <param name="invocations">The invocation options.</param>
    /// <param name="limits">The engine's capability limits.</param>
    public static void Concurrency(StageInvocationOptions invocations, IDictionary<string, int> limits)
    {
        foreach (var (system, gate) in invocations.Systems.Where(entry => entry.Value.MaxConcurrent is > 0))
        {
            limits[$"{AbilityReference.Prefix}{system}.*"] = gate.MaxConcurrent!.Value;
        }
    }

    /// <summary>
    /// Gets how many attempts an ability may use: what it declares, narrowed by the deployment's limit for its system.
    /// </summary>
    /// <param name="descriptor">The ability.</param>
    /// <returns>The attempts.</returns>
    public int Attempts(AbilityDescriptor descriptor) =>
        options.Value.Systems.TryGetValue(descriptor.Reference.System, out var gate) && gate.MaxAttempts is > 0
            ? Math.Min(descriptor.MaxAttempts, gate.MaxAttempts.Value)
            : descriptor.MaxAttempts;

    /// <summary>
    /// Waits until a call to the system is within its rate.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="cancellationToken">Cancelled when the attempt runs out of time.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The limiter did not give room.</exception>
    public async Task Pass(string system, CancellationToken cancellationToken)
    {
        var limiter = _limiters.GetOrAdd(system, Create);
        if (limiter is null) return;
        using var lease = await limiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired) throw new InvalidOperationException($"The rate limit of {system} was not given room.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var limiter in _limiters.Values) limiter?.Dispose();
    }

    RateLimiter? Create(string system)
    {
        if (!options.Value.Systems.TryGetValue(system, out var gate) || gate.RequestsPerSecond is not > 0) return null;
        var rate = gate.RequestsPerSecond.Value;
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = Math.Max(1, (int)Math.Ceiling(rate)),
            TokensPerPeriod = rate >= 1 ? (int)Math.Floor(rate) : 1,
            ReplenishmentPeriod = rate >= 1 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(1 / rate),
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }
}
