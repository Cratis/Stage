// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Takes a settled invocation back to the state entry that made it. Entering the state already happened, so nothing continues from the outcome:
/// a returned or declared outcome completes the entry, and a failure stops it for the operator, who sees it in the invocation ledger.
/// </summary>
/// <param name="logger">The logger.</param>
internal sealed class StateMachineEntryOriginHandler(ILogger<StateMachineEntryOriginHandler> logger) : IInvocationOriginHandler
{
    /// <inheritdoc/>
    public InvocationOriginKind Kind => InvocationOriginKind.StateMachineEntry;

    /// <inheritdoc/>
    public Task Settled(InvocationRecord record)
    {
        if (record.Status == InvocationStatus.Failed || record.Outcome == AbilityDescriptor.FailedOutlet)
        {
            InvocationLog.StateEntryFailed(logger, record.Identity, record.Origin.Address, record.Reason);
        }
        else
        {
            InvocationLog.StateEntrySettled(logger, record.Identity, record.Origin.Address, record.Outcome);
        }

        return Task.CompletedTask;
    }
}
