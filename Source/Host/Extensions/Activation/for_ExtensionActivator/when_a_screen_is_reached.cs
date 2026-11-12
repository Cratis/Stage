// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_a_screen_is_reached : an_activator
{
    IReadOnlyList<ScreenBackend> _reached = null!;
    IReadOnlyList<ScreenBackend> _otherScreen = null!;
    BackendState _stateWhileStarting;
    BackendState _stateAfter;
    ActivationOutcome _firstCall = null!;

    async Task Because()
    {
        await Prepare(string.Empty, [], "Mail", "Billing");
        var screens = new ScreenActivation(new Screens(), _activator);
        _runtime.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Reaching a screen answers at once with the backend starting, and the first call from its component waits for the same start.
        _reached = screens.Reach("/invoices");
        _stateWhileStarting = _activator.Systems.Find("Mail")!.State;
        var call = _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
        _runtime.Hold.SetResult();
        _firstCall = await call;
        await Task.Delay(50);
        _stateAfter = _activator.Systems.Find("Mail")!.State;
        _otherScreen = screens.Reach("/settings");
    }

    [Fact] void should_report_the_backends_of_the_systems_the_screen_places() => _reached.Select(backend => backend.System).ShouldContainOnly("Mail");
    [Fact] void should_not_wait_for_the_backend_to_start() => _reached[0].State.ShouldEqual(BackendState.Starting);
    [Fact] void should_be_starting_before_any_call() => _stateWhileStarting.ShouldEqual(BackendState.Starting);
    [Fact] void should_start_it_once_for_the_screen_and_the_first_call() => _runtime.Starts.Count(launch => launch.System == "Mail").ShouldEqual(1);
    [Fact] void should_have_it_running_when_the_first_call_arrives() => (_firstCall.Endpoint is not null, _stateAfter).ShouldEqual((true, BackendState.Active));
    [Fact] void should_not_start_a_system_the_screen_does_not_place() => _runtime.Starts.Exists(launch => launch.System == "Billing").ShouldBeFalse();
    [Fact] void should_start_nothing_for_a_screen_without_extension_components() => _otherScreen.Count.ShouldEqual(0);

    sealed class Screens : IScreenExtensionUsage
    {
        public IReadOnlyList<string> SystemsOn(string screen) => screen == "/invoices" ? ["Mail", "Unknown"] : [];
    }
}
