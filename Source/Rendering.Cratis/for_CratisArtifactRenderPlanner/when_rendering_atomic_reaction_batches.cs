// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_atomic_reaction_batches : given.an_atomic_reaction
{
    object _reactor = null!;
    EventForEventSourceId[] _effects = [];

    void Because()
    {
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        var assembly = RenderedOutput.Load(Files(_plan));
        var type = assembly.GetType("Reactions.Work.Processing.FollowUp.Worker")!;
        _reactor = Activator.CreateInstance(type)!;
        var started = Activator.CreateInstance(assembly.GetType("Reactions.Work.Processing.FollowUp.Started")!, "one");
        _effects = [.. ((IEnumerable<object>)type.GetMethod("Started")!.Invoke(_reactor, [started, EventContext.EmptyWithEventSourceId("trigger")])!).Cast<EventForEventSourceId>()];
    }

    [Fact] void should_return_every_direct_production_in_order() => _effects.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Recorded", "Audited"]);
    [Fact] void should_preserve_each_destination() => _effects.Select(effect => effect.EventSourceId.Value).ShouldEqual(["one", "audit"]);
    [Fact] void should_preserve_each_payload() => _effects.Select(effect => effect.Event.GetType().GetProperty("Note")!.GetValue(effect.Event)).ShouldEqual(["one", "one"]);
    [Fact] void should_keep_productions_unrouted() => _effects.ShouldEachConformTo(effect => effect.EventSourceType == EventSourceType.Default && effect.EventStreamType == EventStreamType.All && effect.EventStreamId == EventStreamId.Default);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_submit_one_complete_batch_and_propagate_its_outcome(bool rejected)
    {
        var store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        store.EventLog.Returns(log);
        EventForEventSourceId[] submitted = [];
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(call =>
        {
            submitted = [.. call.Arg<IEnumerable<EventForEventSourceId>>()];
            return rejected
                ? AppendManyResult.Failed(CorrelationId.New(), [new AppendError("Batch refused")])
                : AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
        });
        var handler = new EventsForEventSourceIdResultHandler();
        var context = new ReactorContext(EventContext.EmptyWithEventSourceId("trigger"), _reactor, []);
        handler.CanHandle(context, store, _effects).ShouldBeTrue();
        var result = await handler.Handle(context, store, _effects);
        result.IsSuccess.ShouldEqual(!rejected);
        submitted.ShouldEqual(_effects);
        await log.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>());
    }

    [Fact]
    public void should_preserve_all_legacy_artifact_bytes()
    {
        var atomic = _plan;
        SetContext(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { ReactionProductionMode = null }));
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        _plan.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)).ShouldEqual(atomic.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    }

    [Fact]
    public void should_skip_the_complete_batch_for_a_false_guard()
    {
        var type = _reactor.GetType();
        var started = Activator.CreateInstance(type.Assembly.GetType("Reactions.Work.Processing.FollowUp.Started")!, string.Empty);
        var effects = (IEnumerable<object>)type.GetMethod("Started")!.Invoke(_reactor, [started, EventContext.EmptyWithEventSourceId("trigger")])!;
        effects.ShouldBeEmpty();
    }
}
#endif
