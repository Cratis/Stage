// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Describes one ability of a system as the runtime needs it: what it is called, what retrying it may do, which outcomes it
/// can end in, and which extension package realizes it.
/// </summary>
/// <param name="Reference">The ability and its contract major.</param>
/// <param name="Effect">The <see cref="EffectClass"/>.</param>
/// <param name="Outcomes">The names of the outcomes the ability declares besides <c lang="text">returned</c> and <c lang="text">failed</c>.</param>
/// <param name="PackageDigest">The locked digest of the extension package a run pins, so upgrading it never changes an invocation in flight.</param>
public sealed record AbilityDescriptor(AbilityReference Reference, EffectClass Effect, ImmutableArray<string> Outcomes, string PackageDigest)
{
    /// <summary>
    /// Gets the outlet an ability leaves through when it returns without a named outcome.
    /// </summary>
    public const string ReturnedOutlet = "returned";

    /// <summary>
    /// Gets the outlet an ability leaves through when its attempts are used up.
    /// </summary>
    public const string FailedOutlet = "failed";

    /// <summary>
    /// Gets the outlet an ability that answers later leaves through when no answer came in time.
    /// </summary>
    public const string ExpiredOutlet = "expired";

    /// <summary>
    /// Gets how many attempts an invocation may use. Deployment configuration can narrow it per system.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Gets how long one attempt may take.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets how long to wait before the next attempt.
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets how long an answer that arrives later may take. Zero never expires. It has no meaning unless <see cref="AnswersLater"/> is set.
    /// </summary>
    public TimeSpan AnswerExpiresAfter { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether the ability accepts work and answers later, through <see cref="IAbilityInvocations.Answer"/>.
    /// </summary>
    public bool AnswersLater { get; init; }

    /// <summary>
    /// Gets the outlets of the node that performs the ability: <c lang="text">returned</c>, each named outcome, <c lang="text">failed</c>, and <c lang="text">expired</c> for an ability that answers later.
    /// </summary>
    public ImmutableArray<string> Outlets =>
        AnswersLater ? [ReturnedOutlet, .. Outcomes, FailedOutlet, ExpiredOutlet] : [ReturnedOutlet, .. Outcomes, FailedOutlet];
}
