// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class with_overlapping_deliveries : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_deduplicate_overlapping_deliveries_with_the_same_inbox_prefix()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _inbox.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(async Task<IImmutableList<AppendedEvent>> (_) =>
        {
            if (Interlocked.Increment(ref reads) == 2) ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return [];
        });
        await Task.WhenAll(Deliver(Fact(0)), Deliver(Fact(0)));
        _log.Attempts.Count.ShouldEqual(2);
        _log.Committed.Count().ShouldEqual(2);
    }
}
#endif
