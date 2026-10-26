// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class after_a_partial_append : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_append_each_effect_once_and_complete_successfully()
    {
        var fact = Fact(0);
        var effects = await Effects(fact);
        effects.Length.ShouldEqual(2);
        (await _log.Log.AppendMany(effects.Take(1))).IsSuccess.ShouldBeTrue();
        await Deliver(fact);
        _log.Committed.Count().ShouldEqual(2);
        _log.Committed.Select(effect => effect.IdempotencyKey).ShouldEqual(effects.Select(effect => effect.IdempotencyKey));
        _log.Committed.Select(effect => effect.Event).ShouldEqual(effects.Select(effect => effect.Event));
    }
}
#endif
