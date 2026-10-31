// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class and_the_record_is_unchanged : given.a_captured_delivery
{
    async Task Because()
    {
        Render();
        var first = Fact(0);
        await Deliver(first);
        _history.Add(first);
        await Deliver(Fact(1));
    }

    [Fact]
    public void should_not_append_for_an_unchanged_record_at_a_new_inbox_position()
    {
        _log.Attempts.Count.ShouldEqual(1);
        _log.Committed.Count().ShouldEqual(2);
    }
}
#endif
