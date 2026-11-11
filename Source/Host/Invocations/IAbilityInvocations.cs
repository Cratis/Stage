// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Starts ability invocations from their origins and settles the ones that wait: this is the surface reactions, workflows, state machines and the extension protocol use.
/// </summary>
public interface IAbilityInvocations
{
    /// <summary>
    /// Records an invocation with its cause and starts its run. Doing it again for the same origin and causation finds the invocation that exists.
    /// </summary>
    /// <param name="request">The <see cref="AbilityInvocation"/>.</param>
    /// <returns>The <see cref="InvocationAdmission"/>.</returns>
    Task<InvocationAdmission> Invoke(AbilityInvocation request);

    /// <summary>
    /// Answers an invocation that waits, after checking the identity that answers against the persona or policy the invocation names.
    /// </summary>
    /// <param name="identity">The invocation identity, which is also the key the answer is addressed with.</param>
    /// <param name="outlet">The outlet the answer takes: <c lang="text">returned</c> or a declared outcome.</param>
    /// <param name="payload">What the answer carries, as JSON.</param>
    /// <param name="answeredBy">The identity that answers. It is recorded with the invocation.</param>
    /// <returns>What came of it.</returns>
    Task<InvocationAnswer> Answer(string identity, string outlet, string payload, ClaimsPrincipal answeredBy);

    /// <summary>
    /// Records which outcome happened for an invocation that stopped for reconciliation, and continues the origin from it.
    /// </summary>
    /// <param name="identity">The invocation identity.</param>
    /// <param name="outcome">The outcome that happened: <c lang="text">returned</c>, a declared outcome, or <c lang="text">failed</c>.</param>
    /// <param name="output">What the ability returned, as JSON.</param>
    /// <param name="actor">The operator who found out what happened.</param>
    /// <returns>What came of it.</returns>
    Task<InvocationReconciliation> Reconcile(string identity, string outcome, string output, string actor);

    /// <summary>
    /// Lists invocations, newest first.
    /// </summary>
    /// <param name="status">The status to keep, or null for every status.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<InvocationRecord>> List(InvocationStatus? status = null);
}
