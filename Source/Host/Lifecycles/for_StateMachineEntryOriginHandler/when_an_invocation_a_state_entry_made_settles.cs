// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.Lifecycles.for_StateMachineEntryOriginHandler;

public class when_an_invocation_a_state_entry_made_settles : Specification
{
    StateMachineEntryOriginHandler _handler = null!;
    Exception? _returned;
    Exception? _failed;

    void Establish() => _handler = new(NullLogger<StateMachineEntryOriginHandler>.Instance);

    async Task Because()
    {
        var origin = new InvocationOrigin(InvocationOriginKind.StateMachineEntry, "InvoiceLifecycle/Sent/entering 1", "key:entered:InvoiceLifecycle:Sent:4");
        var record = new InvocationRecord("invocation-1", "ability:Billing.Charge", EffectClass.Safe, "sha256:aaaa", origin, InvocationStatus.Settled, DateTimeOffset.UtcNow) { Outcome = AbilityDescriptor.ReturnedOutlet };
        _returned = await Record(record);
        _failed = await Record(record with { Status = InvocationStatus.Failed, Outcome = AbilityDescriptor.FailedOutlet, Reason = "No answer" });
    }

    async Task<Exception?> Record(InvocationRecord record)
    {
        try
        {
            await _handler.Settled(record);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Fact] void should_take_the_state_entry_origin() => _handler.Kind.ShouldEqual(InvocationOriginKind.StateMachineEntry);
    [Fact] void should_complete_a_returned_invocation() => _returned.ShouldBeNull();
    [Fact] void should_leave_a_failed_invocation_to_the_operator_without_failing_the_settlement() => _failed.ShouldBeNull();
}
