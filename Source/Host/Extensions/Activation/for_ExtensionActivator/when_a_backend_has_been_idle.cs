// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_a_backend_has_been_idle : an_activator
{
    BackendState _afterFiveMinutes;
    BackendState _whileBusy;
    BackendState _afterTheTimeout;
    ActivationOutcome _next = null!;
    BackendSlot _slot = null!;

    async Task Because()
    {
        await Prepare("activation on demand idle after 10 minutes", [], "Mail");
        _slot = _activator.Systems.Find("Mail")!;
        await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);

        Idle(TimeSpan.FromMinutes(5));
        await _activator.Sweep(CancellationToken.None);
        _afterFiveMinutes = _slot.State;

        // A call that takes longer than the timeout must not lose its backend.
        using (_activator.Begin("Mail"))
        {
            Idle(TimeSpan.FromMinutes(30));
            await _activator.Sweep(CancellationToken.None);
            _whileBusy = _slot.State;
        }

        Idle(TimeSpan.FromMinutes(11));
        await _activator.Sweep(CancellationToken.None);
        _afterTheTimeout = _slot.State;
        _next = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
    }

    [Fact] void should_keep_it_before_the_timeout() => _afterFiveMinutes.ShouldEqual(BackendState.Active);
    [Fact] void should_keep_it_while_a_call_is_in_flight() => _whileBusy.ShouldEqual(BackendState.Active);
    [Fact] void should_stop_it_after_the_timeout() => _afterTheTimeout.ShouldEqual(BackendState.Inactive);
    [Fact] void should_stop_the_backend() => _runtime.Backends[0].Stopped.ShouldBeTrue();
    [Fact] void should_let_go_of_the_connection() => _stopped.ShouldContainOnly("Mail");
    [Fact] void should_record_when_it_stopped() => _slot.LastStopped.ShouldNotBeNull();
    [Fact] void should_start_it_again_for_the_next_call() => _runtime.Starts.Count.ShouldEqual(2);
    [Fact] void should_answer_the_next_call_with_the_new_backend() => _next.Endpoint!.Address.ShouldEqual(_runtime.Starts[1].Address);
    [Fact] void should_be_active_again() => _slot.State.ShouldEqual(BackendState.Active);
}
