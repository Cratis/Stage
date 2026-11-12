// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_the_policy_keeps_a_backend_running : an_activator
{
    BackendState _started;
    BackendState _longIdle;
    BackendState _afterCrash;
    BackendState _overriddenToOnDemand;
    BackendState _overriddenToAlways;
    int _startsBeforeCrash;

    async Task Because()
    {
        await Prepare(
            "activation always",
            new()
            {
                ["Relaxed"] = new(false, 5, "minutes")
            },
            "Mail",
            "Relaxed");

        // The contract says always on and nobody called: the sweep starts it.
        await _activator.Sweep(CancellationToken.None);
        _started = _activator.Systems.Find("Mail")!.State;
        _overriddenToOnDemand = _activator.Systems.Find("Relaxed")!.State;

        Idle(TimeSpan.FromDays(2));
        await _activator.Sweep(CancellationToken.None);
        _longIdle = _activator.Systems.Find("Mail")!.State;
        _startsBeforeCrash = _runtime.Starts.Count(launch => launch.System == "Mail");

        _runtime.Backends.Single(backend => backend.System == "Mail").Crash("it fell over");
        await Task.Delay(50);
        await _activator.Sweep(CancellationToken.None);
        _afterCrash = _activator.Systems.Find("Mail")!.State;

        await _activator.Activate("Relaxed", ActivationReason.Invocation, CancellationToken.None);
        Idle(TimeSpan.FromMinutes(6));
        await _activator.Sweep(CancellationToken.None);
        _overriddenToAlways = _activator.Systems.Find("Relaxed")!.State;
    }

    [Fact] void should_start_an_always_on_backend_without_a_call() => _started.ShouldEqual(BackendState.Active);
    [Fact] void should_not_start_a_system_the_consumer_made_on_demand() => _overriddenToOnDemand.ShouldEqual(BackendState.Inactive);
    [Fact] void should_never_stop_an_always_on_backend_for_being_idle() => _longIdle.ShouldEqual(BackendState.Active);
    [Fact] void should_start_an_always_on_backend_that_failed_again() => (_afterCrash, _runtime.Starts.Count(launch => launch.System == "Mail")).ShouldEqual((BackendState.Active, _startsBeforeCrash + 1));
    [Fact] void should_stop_a_backend_the_consumer_made_on_demand_when_it_is_idle() => _overriddenToAlways.ShouldEqual(BackendState.Inactive);
}
