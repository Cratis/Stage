// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivator;

public class when_a_call_needs_a_backend : an_activator
{
    BackendState _before;
    ActivationOutcome _first = null!;
    ActivationOutcome _second = null!;
    ActivationOutcome _frontendOnly = null!;
    BackendSlot _slot = null!;

    async Task Because()
    {
        await Prepare(string.Empty, [], "Mail");
        _slot = _activator.Systems.Find("Mail")!;
        _before = _slot.State;
        _first = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
        _second = await _activator.Activate("Mail", ActivationReason.Invocation, CancellationToken.None);
        _frontendOnly = await _activator.Activate("Unknown", ActivationReason.Invocation, CancellationToken.None);
    }

    [Fact] void should_not_start_anything_before_the_first_need() => _before.ShouldEqual(BackendState.Inactive);
    [Fact] void should_start_the_backend_once() => _runtime.Starts.Count.ShouldEqual(1);
    [Fact] void should_start_it_in_the_tier_its_plan_names() => _runtime.Starts[0].Plan.Tier.ShouldEqual(ExecutionTier.SharedHost);
    [Fact] void should_answer_where_the_backend_listens() => _first.Endpoint!.Address.ShouldEqual(_runtime.Starts[0].Address);
    [Fact] void should_pin_the_endpoint_to_the_locked_package() => _first.Endpoint!.PackageDigest.ShouldEqual(_entry.Digest);
    [Fact] void should_be_active() => _slot.State.ShouldEqual(BackendState.Active);
    [Fact] void should_record_when_it_was_activated() => _slot.LastActivation.ShouldEqual(_time.GetUtcNow());
    [Fact] void should_reuse_the_running_backend() => _second.Endpoint!.Address.ShouldEqual(_first.Endpoint!.Address);
    [Fact] void should_know_no_backend_for_a_system_without_an_extension() => _frontendOnly.Endpoint.ShouldBeNull();
}
