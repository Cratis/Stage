// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class after_a_refused_append : given.a_captured_delivery
{
    void Because() => Render();

    [Fact]
    public async Task should_fail_without_an_advance_when_the_destination_refuses_the_batch()
    {
        _log.Refusal = AppendManyResult.Failed(CorrelationId.New(), [new AppendError("Refused")]);
        var reactor = Reactor();
        var fact = Fact(0);
        var delivery = ReactorDelivery.For(reactor, fact.Context);
        var effects = await (Task<IEnumerable<object>>)reactor.GetType().GetMethod("OnArrived")!.Invoke(reactor, [fact.Content, fact.Context, delivery])!;
        var result = await new EventsForEventSourceIdResultHandler().Handle(new ReactorContext(fact.Context, reactor, []) { Delivery = delivery }, _store, effects);
        result.IsSuccess.ShouldBeFalse();
        _log.Committed.ShouldBeEmpty();
        _log.Refusal = null;
        await Deliver(Fact(0));
        _log.Committed.Count().ShouldEqual(2);
        _log.Attempts[0].Select(effect => effect.IdempotencyKey).ShouldEqual(_log.Attempts[1].Select(effect => effect.IdempotencyKey));
    }
}
#endif
