// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Extensions;

/// <summary>
/// What an ability answers for an attempt. Create one with <see cref="Returned"/>, <see cref="Accepted"/>, <see cref="NotSent"/> or <see cref="Failed"/>.
/// </summary>
public abstract record AbilityOutcome
{
    /// <summary>
    /// The system did the work.
    /// </summary>
    /// <param name="outputJson">What the ability returns, as JSON. An empty object when omitted.</param>
    /// <param name="outcome">The named outcome the ability declares, or null for a plain return.</param>
    /// <returns>The <see cref="AbilityOutcome"/>.</returns>
    public static AbilityOutcome Returned(string outputJson = "{}", string? outcome = null) => new ReturnedOutcome(outcome ?? string.Empty, outputJson);

    /// <summary>
    /// The system accepted the work and will answer later, which the extension then does with <see cref="HostServices.Answer"/>.
    /// </summary>
    /// <returns>The <see cref="AbilityOutcome"/>.</returns>
    public static AbilityOutcome Accepted() => new AcceptedOutcome();

    /// <summary>
    /// The call certainly did not reach the system, so nothing happened and it is safe to try again, even for a once ability.
    /// </summary>
    /// <param name="reason">Why it did not reach the system.</param>
    /// <returns>The <see cref="AbilityOutcome"/>.</returns>
    public static AbilityOutcome NotSent(string reason) => new NotSentOutcome(reason);

    /// <summary>
    /// The call failed.
    /// </summary>
    /// <param name="reason">What went wrong.</param>
    /// <param name="effectRuledOut">True only when the extension can rule out that the effect happened. Leave it false when unsure, which is what an exception or a timeout means.</param>
    /// <returns>The <see cref="AbilityOutcome"/>.</returns>
    public static AbilityOutcome Failed(string reason, bool effectRuledOut = false) => new FailedOutcome(reason, effectRuledOut);

    internal sealed record ReturnedOutcome(string Outcome, string OutputJson) : AbilityOutcome;

    internal sealed record AcceptedOutcome : AbilityOutcome;

    internal sealed record NotSentOutcome(string Reason) : AbilityOutcome;

    internal sealed record FailedOutcome(string Reason, bool EffectRuledOut) : AbilityOutcome;
}
