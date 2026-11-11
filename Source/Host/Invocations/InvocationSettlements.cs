// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Delivers settled invocations to the <see cref="IInvocationOriginHandler"/> of their origin.
/// </summary>
/// <param name="handlers">The handlers.</param>
[IgnoreConvention]
public sealed class InvocationSettlements(IEnumerable<IInvocationOriginHandler> handlers)
{
    readonly Dictionary<InvocationOriginKind, IInvocationOriginHandler> _handlers = handlers.ToDictionary(handler => handler.Kind);

    /// <summary>
    /// Delivers a settled invocation to its origin. Failing here fails the settlement node, which the engine retries.
    /// </summary>
    /// <param name="record">The settled invocation.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">No handler serves the kind of origin of the invocation.</exception>
    public Task Deliver(InvocationRecord record) =>
        _handlers.TryGetValue(record.Origin.Kind, out var handler)
            ? handler.Settled(record)
            : throw new InvalidOperationException($"Nothing takes the outcome of invocation {record.Identity} back to its origin: no handler serves {record.Origin.Kind}.");
}
