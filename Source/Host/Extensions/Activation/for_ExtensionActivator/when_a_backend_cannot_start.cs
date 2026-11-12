// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_a_backend_cannot_start : an_activator
{
    ActivationOutcome _failed = null!;
    ActivationOutcome _retried = null!;
    BackendState _stateAfterFailure;
    string _refusalAfterFailure = string.Empty;
    ActivationOutcome _unplaceable = null!;

    async Task Because()
    {
        await Prepare(string.Empty, [], "Mail", "Untrusted");
        _runtime.Failure = "the process did not listen";
        _failed = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
        _stateAfterFailure = _activator.Systems.Find("Mail")!.State;
        _refusalAfterFailure = _activator.Status().Single(status => status.System == "Mail").Refusal;

        _runtime.Failure = null;
        _retried = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);

        _options.Execution.Sharing.Publishers = [];
        _unplaceable = await new ExtensionActivator(
            new(
                [new Packages.ExtensionPackages(_result, new Dictionary<string, string> { ["Untrusted"] = Packages.given.PackageFixtures.Reference })],
                [],
                Microsoft.Extensions.Options.Options.Create(_options)),
            [_runtime],
            Microsoft.Extensions.Options.Options.Create(_options),
            _time,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ExtensionActivator>.Instance).Activate("Untrusted", ActivationReason.Invocation, CancellationToken.None);
    }

    [Fact] void should_refuse_with_the_reason() => (_failed.Endpoint, _failed.Refusal).ShouldEqual((null, "the process did not listen"));
    [Fact] void should_stay_inactive() => _stateAfterFailure.ShouldEqual(BackendState.Inactive);
    [Fact] void should_show_the_reason_in_the_status() => _refusalAfterFailure.ShouldEqual("the process did not listen");
    [Fact] void should_try_again_on_the_next_need() => (_retried.Endpoint is not null, _runtime.Starts.Count).ShouldEqual((true, 2));
    [Fact] void should_refuse_a_backend_the_deployment_has_no_place_for() => (_unplaceable.Endpoint, _unplaceable.Refusal.Contains("Containers:Untrusted", StringComparison.Ordinal)).ShouldEqual((null, true));
}
