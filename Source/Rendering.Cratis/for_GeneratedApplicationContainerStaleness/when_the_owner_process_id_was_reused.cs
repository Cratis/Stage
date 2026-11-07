// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness;

public class when_the_owner_process_id_was_reused : a_candidate
{
    StalenessDecision _decision = null!;

    void Establish() => _processes.Result = OwnerProcessState.Replaced;

    void Because() => _decision = Decide(Started("host-a", TimeSpan.FromMinutes(5)));

    [Fact] void should_reap_it() => _decision.Reap.ShouldBeTrue();
}
