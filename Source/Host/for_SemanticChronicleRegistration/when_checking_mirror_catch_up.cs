// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Specifications;
using Xunit;

using RunningState = Cratis.Chronicle.Contracts.Observation.ObserverRunningState;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_checking_mirror_catch_up : Specification
{
    ObserverInformation _observer = null!;

    void Establish() => _observer = new ObserverInformation
    {
        IsSubscribed = true,
        RunningState = RunningState.Active,
        LastHandledEventSequenceNumber = ulong.MaxValue
    };

    [Fact] void should_not_treat_unavailable_last_handled_as_progress() => SemanticChronicleRegistration.IsCaughtUp(_observer, 0).ShouldBeFalse();
    [Fact] void should_accept_an_event_type_without_prior_events() => SemanticChronicleRegistration.IsCaughtUp(_observer, ulong.MaxValue).ShouldBeTrue();
    [Fact] void should_refuse_a_replaying_mirror()
    {
        _observer.RunningState = RunningState.Replaying;
        SemanticChronicleRegistration.IsCaughtUp(_observer, ulong.MaxValue).ShouldBeFalse();
    }
}
