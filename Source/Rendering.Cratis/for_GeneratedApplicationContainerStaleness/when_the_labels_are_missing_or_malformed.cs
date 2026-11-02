// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness;

public class when_the_labels_are_missing_or_malformed : a_candidate
{
    StalenessDecision _decision = null!;

    void Establish() => _processes.Result = OwnerProcessState.Dead;

    void Because() => _decision = Decide(null);

    [Fact] void should_keep_it() => _decision.Reap.ShouldBeFalse();
    [Fact] void should_say_why() => _decision.Reason.ShouldContain("missing or malformed");
}
