// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The persisted record of one ability invocation: what it is, where it came from, how far it got, and how it ended.
/// </summary>
/// <param name="Identity">The invocation identity, see <see cref="InvocationIdentity"/>.</param>
/// <param name="Capability">The capability the invocation runs under, including the contract major.</param>
/// <param name="Effect">The <see cref="EffectClass"/>.</param>
/// <param name="PackageDigest">The extension package digest the invocation pinned.</param>
/// <param name="Origin">The <see cref="InvocationOrigin"/>.</param>
/// <param name="Status">The <see cref="InvocationStatus"/>.</param>
/// <param name="Occurred">When the invocation was recorded.</param>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationRecord")]
public sealed record InvocationRecord(
    [property: Id(0)] string Identity,
    [property: Id(1)] string Capability,
    [property: Id(2)] EffectClass Effect,
    [property: Id(3)] string PackageDigest,
    [property: Id(4)] InvocationOrigin Origin,
    [property: Id(5)] InvocationStatus Status,
    [property: Id(6)] DateTimeOffset Occurred)
{
    /// <summary>
    /// Gets the run that executes the invocation, as a workflow run key. Empty until a run exists.
    /// </summary>
    [Id(7)]
    public string Run { get; init; } = string.Empty;

    /// <summary>
    /// Gets the node execution that performs the ability, as a node execution key. Empty until an attempt has begun.
    /// </summary>
    [Id(8)]
    public string Execution { get; init; } = string.Empty;

    /// <summary>
    /// Gets how many attempts have begun.
    /// </summary>
    [Id(9)]
    public int Attempts { get; init; }

    /// <summary>
    /// Gets the attempt that has begun and has no outcome, or zero. A <see cref="EffectClass.Once"/> invocation that finds this set again
    /// does not call the system a second time.
    /// </summary>
    [Id(10)]
    public int AttemptInFlight { get; init; }

    /// <summary>
    /// Gets the outcome the invocation ended in: <c lang="text">returned</c>, a named outcome, or <c lang="text">failed</c>.
    /// </summary>
    [Id(11)]
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    /// Gets what the ability returned, as JSON.
    /// </summary>
    [Id(12)]
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Gets why the invocation failed, is retrying or stopped.
    /// </summary>
    [Id(13)]
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets the policy the identity that answers must satisfy, or empty when any authenticated identity may answer.
    /// </summary>
    [Id(14)]
    public string AnswerPolicy { get; init; } = string.Empty;

    /// <summary>
    /// Gets who answered or reconciled the invocation. Empty when nobody did.
    /// </summary>
    [Id(15)]
    public string DecidedBy { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the invocation ended.
    /// </summary>
    [Id(16)]
    public DateTimeOffset? Settled { get; init; }
}
