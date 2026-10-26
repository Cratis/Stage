// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class after_a_lost_acknowledgment : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_not_duplicate_effects_after_commit_when_the_acknowledgment_is_lost()
    {
        var fact = Fact(0);
        _log.LoseNextAcknowledgment = true;
        await Assert.ThrowsAsync<given.a_keyed_capture_log.LostAcknowledgment>(() => Deliver(fact));
        _log.Committed.Count().ShouldEqual(2);
        await Deliver(fact);
        _log.Attempts.Count.ShouldEqual(2);
        _log.Committed.Count().ShouldEqual(2);
        _log.Attempts[0].Select(effect => effect.IdempotencyKey).ShouldEqual(_log.Attempts[1].Select(effect => effect.IdempotencyKey));
    }
}
#endif
