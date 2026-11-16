// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Lifecycles.for_StateEntryAbilities;

public class when_the_ledger_refuses_the_invocation : given.a_state_entry_that_invokes_an_ability
{
    Exception? _error;

    void Establish() => _invocations.Invoke(Arg.Any<AbilityInvocation>()).Returns(new InvocationAdmission(false, null, "The ledger is in memory."));

    async Task Because()
    {
        try
        {
            await _abilities.Invoke(_entry, _subject);
        }
        catch (InvalidOperationException exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_fail_the_entering_loudly() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_say_why() => _error!.Message.ShouldContain("The ledger is in memory.");
}
