// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class and_the_foreign_record_changes : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_key_each_effect_and_each_new_delivery_separately()
    {
        var first = Fact(0);
        await Deliver(first);
        _history.Add(first);
        await Deliver(Fact(1, status: "delivered"));
        _log.Committed.Count().ShouldEqual(4);
        _log.Committed.Select(effect => effect.IdempotencyKey).Distinct().Count().ShouldEqual(4);
        _log.Committed.ShouldEachConformTo(effect => effect.EventSourceId.Value == "parcel" && effect.Tags.Contains("captured"));
        _log.Committed.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Added", "StatusRecorded", "Added", "StatusRecorded"]);
    }
}
#endif
