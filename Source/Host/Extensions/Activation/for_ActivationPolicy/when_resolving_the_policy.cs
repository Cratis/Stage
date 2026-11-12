// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ActivationPolicy;

public class when_resolving_the_policy : Specification
{
    static readonly TimeSpan _default = TimeSpan.FromMinutes(10);
    static readonly SourceLocation _location = default!;

    ActivationPolicy _nothing = null!;
    ActivationPolicy _contractAlways = null!;
    ActivationPolicy _contractIdle = null!;
    ActivationPolicy _consumerAlways = null!;
    ActivationPolicy _consumerIdle = null!;
    ActivationPolicy _consumerOnDemand = null!;

    void Because()
    {
        _nothing = ActivationPolicy.Resolve(null, null, _default);
        _contractAlways = ActivationPolicy.Resolve(new(true, null, null, _location), null, _default);
        _contractIdle = ActivationPolicy.Resolve(new(false, 5, "minute", _location), null, _default);
        _consumerAlways = ActivationPolicy.Resolve(new(false, 5, "minute", _location), new(true, null, null), _default);
        _consumerIdle = ActivationPolicy.Resolve(new(true, null, null, _location), new(false, 2, "hours"), _default);
        _consumerOnDemand = ActivationPolicy.Resolve(new(false, 5, "minute", _location), new(false, null, null), _default);
    }

    [Fact] void should_be_on_demand_with_the_deployments_idle_timeout_when_nobody_says() => (_nothing.Always, _nothing.IdleTimeout).ShouldEqual((false, _default));
    [Fact] void should_keep_an_always_on_contract_running() => _contractAlways.Always.ShouldBeTrue();
    [Fact] void should_use_the_idle_timeout_the_contract_states() => _contractIdle.IdleTimeout.ShouldEqual(TimeSpan.FromMinutes(5));
    [Fact] void should_let_the_consumer_keep_a_backend_running() => _consumerAlways.Always.ShouldBeTrue();
    [Fact] void should_let_the_consumer_stop_an_always_on_backend_when_idle() => (_consumerIdle.Always, _consumerIdle.IdleTimeout).ShouldEqual((false, TimeSpan.FromHours(2)));
    [Fact] void should_keep_the_contracts_idle_timeout_when_the_consumer_states_none() => (_consumerOnDemand.Always, _consumerOnDemand.IdleTimeout).ShouldEqual((false, TimeSpan.FromMinutes(5)));
}
