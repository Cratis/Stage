// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness;

public class when_the_container_belongs_to_the_current_run : a_candidate
{
    StalenessDecision _decision = null!;

    void Because() => _decision = Decide(Started("host-a", TimeSpan.FromHours(5), "current0"));

    [Fact] void should_keep_it() => _decision.Reap.ShouldBeFalse();
}
