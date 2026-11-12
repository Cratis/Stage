// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The consumer's <c lang="text">activation</c> line on a system, which overrides the extension's default.
/// </summary>
/// <param name="Always">Whether the backend stays running.</param>
/// <param name="IdleAmount">How many units the backend may be idle before it stops, when stated.</param>
/// <param name="IdleUnit">The unit of <paramref name="IdleAmount"/>: seconds, minutes, hours or days, singular or plural.</param>
public sealed record ActivationOverride(bool Always, int? IdleAmount, string? IdleUnit);

/// <summary>
/// When a backend runs: always, or from its first need until it has been idle for a while.
/// </summary>
/// <param name="Always">Whether the backend stays running. It is never stopped for being idle.</param>
/// <param name="IdleTimeout">How long an on-demand backend may be idle before it stops.</param>
public sealed record ActivationPolicy(bool Always, TimeSpan IdleTimeout)
{
    /// <summary>
    /// Settles the policy of a system. The consumer's line wins over the extension's default, which wins over the deployment's idle timeout.
    /// </summary>
    /// <param name="contract">The default the extension's contract states, or null when it states none.</param>
    /// <param name="consumer">The system's own <c lang="text">activation</c> line, or null.</param>
    /// <param name="defaultIdle">The idle timeout of the deployment, used when neither states one.</param>
    /// <returns>The <see cref="ActivationPolicy"/>.</returns>
    public static ActivationPolicy Resolve(SystemActivationSyntax? contract, ActivationOverride? consumer, TimeSpan defaultIdle)
    {
        var fromContract = contract is null ? new ActivationPolicy(false, defaultIdle) : From(contract.Always, contract.IdleAmount, contract.IdleUnit, defaultIdle);
        if (consumer is null) return fromContract;

        var idle = consumer.IdleAmount is null ? fromContract.IdleTimeout : Duration(consumer.IdleAmount.Value, consumer.IdleUnit, defaultIdle);
        return new(consumer.Always, idle);
    }

    /// <summary>
    /// Turns an amount and a unit into a duration.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="unit">The unit, singular or plural. An unknown unit is minutes.</param>
    /// <param name="fallback">What an amount below one gives.</param>
    /// <returns>The duration.</returns>
    public static TimeSpan Duration(int amount, string? unit, TimeSpan fallback)
    {
        if (amount < 1) return fallback;
        return (unit ?? string.Empty).TrimEnd('s').ToLower(CultureInfo.InvariantCulture) switch
        {
            "second" => TimeSpan.FromSeconds(amount),
            "hour" => TimeSpan.FromHours(amount),
            "day" => TimeSpan.FromDays(amount),
            _ => TimeSpan.FromMinutes(amount)
        };
    }

    /// <inheritdoc/>
    public override string ToString() => Always ? "always" : $"on demand, idle after {IdleTimeout.ToString("c", CultureInfo.InvariantCulture)}";

    static ActivationPolicy From(bool always, int? amount, string? unit, TimeSpan defaultIdle) =>
        new(always, amount is null ? defaultIdle : Duration(amount.Value, unit, defaultIdle));
}
