// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Takes the outcome of an invocation back to the behavior that asked for it: a reaction branch settles, a workflow takes its outlet, a state
/// machine records the effect as done. The behaviors register one handler for their <see cref="InvocationOriginKind"/>.
/// </summary>
/// <remarks>
/// Delivery is at least once: the same settled invocation may be delivered again after a failure or a restart, and a handler finds what it already did
/// by <see cref="InvocationRecord.Identity"/>.
/// </remarks>
public interface IInvocationOriginHandler
{
    /// <summary>
    /// Gets the kind of origin the handler serves.
    /// </summary>
    InvocationOriginKind Kind { get; }

    /// <summary>
    /// Takes a settled invocation back to its origin.
    /// </summary>
    /// <param name="record">The settled <see cref="InvocationRecord"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Settled(InvocationRecord record);
}
