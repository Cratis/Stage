// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The outbox of ability invocations: every invocation is recorded here with its cause before anything runs, and the ledger keeps the guard that stops a
/// <see cref="EffectClass.Once"/> effect from repeating.
/// </summary>
public interface IInvocationLedger
{
    /// <summary>
    /// Records an invocation with the fact, observer offset or run that caused it. An invocation that is recorded is returned as it is.
    /// </summary>
    /// <param name="descriptor">The ability.</param>
    /// <param name="origin">The <see cref="InvocationOrigin"/>.</param>
    /// <param name="answerPolicy">The policy an identity that answers must satisfy, or empty.</param>
    /// <returns>The record.</returns>
    Task<InvocationRecord> Record(AbilityDescriptor descriptor, InvocationOrigin origin, string answerPolicy = "");

    /// <summary>
    /// Gets the grain of an invocation.
    /// </summary>
    /// <param name="identity">The invocation identity.</param>
    /// <returns>The <see cref="IInvocationGrain"/>.</returns>
    IInvocationGrain Of(string identity);

    /// <summary>
    /// Gets an invocation.
    /// </summary>
    /// <param name="identity">The invocation identity.</param>
    /// <returns>The record, or null when it was never recorded.</returns>
    Task<InvocationRecord?> Get(string identity);

    /// <summary>
    /// Lists invocations, newest first.
    /// </summary>
    /// <param name="status">The status to keep, or null for every status.</param>
    /// <param name="limit">The most to list.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<InvocationRecord>> List(InvocationStatus? status = null, int limit = 200);
}
