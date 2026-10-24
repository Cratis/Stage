// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class after_a_full_append_without_a_checkpoint : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_append_each_effect_once_and_complete_successfully()
    {
        var fact = Fact(0);
        await Deliver(fact);
        var first = _log.Committed.ToArray();
        first.Length.ShouldEqual(2);
        await Deliver(fact);
        _log.Committed.ShouldEqual(first);
        _log.Attempts.Count.ShouldEqual(2);
    }
}
#endif
