// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class with_an_earlier_append_guard_filtered_out : given.a_captured_delivery
{
    void Establish() => _model = Transform(_model, slice => slice with
    {
        Captures = [.. slice.Captures.Select(capture => capture with
        {
            Appends = capture.Appends.SetItem(0, capture.Appends[0] with { When = new(SemanticCaptureConditionKind.Added, []) })
        })]
    });

    EventForEventSourceId _effect = null!;
    ReactorDelivery _delivery = null!;

    async Task Because()
    {
        Render();
        var first = Fact(0);
        (await Effects(first)).Length.ShouldEqual(2);
        _history.Add(first);
        var changed = Fact(1, "delivered");
        _effect = (await Effects(changed)).Single();
        _delivery = ReactorDelivery.For(Reactor(), changed.Context);
    }

    [Fact]
    public void should_key_by_the_modeled_append_position_not_the_returned_list_position()
    {
        _effect.IdempotencyKey.ShouldEqual(_delivery.IdempotencyKeyFor($"{_delivery.Reactor}/1"));
    }
}
#endif
