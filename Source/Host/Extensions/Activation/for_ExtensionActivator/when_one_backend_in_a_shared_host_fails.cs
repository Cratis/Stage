// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_one_backend_in_a_shared_host_fails : an_activator
{
    BackendSlot _failing = null!;
    BackendSlot _other = null!;
    ActivationOutcome _stillServing = null!;
    ActivationOutcome _restarted = null!;

    async Task Because()
    {
        await Prepare(string.Empty, [], "Mail", "Billing");
        _failing = _activator.Systems.Find("Mail")!;
        _other = _activator.Systems.Find("Billing")!;
        await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
        await _activator.Activate("Billing", ActivationReason.Invocation, CancellationToken.None);

        _runtime.Backends.Single(backend => backend.System == "Mail").Crash("unhandled exception");
        await Task.Delay(50);
        _stillServing = await _activator.Activate("Billing", ActivationReason.Invocation, CancellationToken.None);
        _restarted = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
    }

    [Fact] void should_not_stop_the_other_extension() => (_other.State, _runtime.Backends.Single(backend => backend.System == "Billing").Stopped).ShouldEqual((BackendState.Active, false));
    [Fact] void should_keep_serving_the_other_extension_where_it_was() => _stillServing.Endpoint!.Address.ShouldEqual(_other.Address!);
    [Fact] void should_not_start_the_other_extension_again() => _runtime.Starts.Count(launch => launch.System == "Billing").ShouldEqual(1);
    [Fact] void should_let_go_of_the_connection_to_the_one_that_failed() => _stopped.ShouldContain("Mail");
    [Fact] void should_start_the_one_that_failed_again_when_it_is_needed() => (_restarted.Endpoint is not null, _runtime.Starts.Count(launch => launch.System == "Mail")).ShouldEqual((true, 2));
}
