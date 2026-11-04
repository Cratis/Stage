// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness;

public class when_the_owner_is_on_another_host : a_candidate
{
    StalenessDecision _decision = null!;

    void Establish() => _processes.Result = OwnerProcessState.Dead;

    void Because() => _decision = Decide(Started("host-b", TimeSpan.FromMinutes(5)));

    [Fact] void should_keep_it_because_liveness_is_only_judged_on_this_host() => _decision.Reap.ShouldBeFalse();
}
