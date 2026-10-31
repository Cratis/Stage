// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_an_unmarked_key : given.a_captured_delivery
{
    EventForEventSourceId[] _effects = [];

    async Task Because()
    {
        Render();
        _history.Add(Fact(0, source: "another"));
        _history.Add(Fact(1, "different", key: "another-key"));
        _history.Add(Fact(2, "equal"));
        _history.Add(Fact(3, "after"));
        _effects = await Effects(Fact(2));
    }

    [Fact]
    public async Task should_keep_reading_across_sources_and_matching_by_key()
    {
        _effects.ShouldBeEmpty();
        await _inbox.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, null, Arg.Any<IEnumerable<EventType>>());
    }
}
#endif
