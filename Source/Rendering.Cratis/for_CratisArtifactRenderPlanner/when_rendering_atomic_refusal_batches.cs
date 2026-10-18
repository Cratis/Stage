// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Chronicle.Reactors;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_atomic_refusal_batches : given.an_atomic_reaction
{
    CommandInvocation _invocation = null!;
    EventForEventSourceId[] _batch = [];

    void Establish() => SetContext(Compile("reaction batches atomic\n" + Refusals));

    void Because()
    {
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        var assembly = RenderedOutput.Load(Files(_plan));
        var type = assembly.GetType("Reactions.Work.Processing.FollowUp.Worker")!;
        var reactor = Activator.CreateInstance(type)!;
        var started = Activator.CreateInstance(assembly.GetType("Reactions.Work.Processing.FollowUp.Started")!, "one");
        var effects = (IEnumerable<object>)type.GetMethod("Started")!.Invoke(reactor, [started, EventContext.EmptyWithEventSourceId("trigger")])!;
        var effect = effects.Single();
        effect.ShouldBeOfExactType<CommandInvocation>();
        _invocation = (CommandInvocation)effect;
        var refusal = new CommandRefusal(CommandRefusalKind.Validation, null, "Invalid id");
        _batch = [.. ((object[])ProduceRefusal(refusal)!).Cast<EventForEventSourceId>()];
    }

    [Fact] void should_keep_the_invoked_command_outside_the_event_batch() => _invocation.Command.GetType().Name.ShouldEqual("Complete");
    [Fact] void should_return_the_selected_branch_as_one_event_collection() => _batch.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Recorded", "Audited"]);
    [Fact] void should_preserve_the_trigger_and_explicit_destinations() => _batch.Select(effect => effect.EventSourceId.Value).ShouldEqual(["trigger", "audit"]);
    [Fact] void should_preserve_refusal_payloads() => _batch.Select(effect => effect.Event.GetType().GetProperty("Note")!.GetValue(effect.Event)).ShouldEqual(["Invalid id", "validation"]);

    [Fact]
    public void should_acknowledge_a_constraint_refusal_without_an_event_batch()
    {
        var refusal = new CommandRefusal(CommandRefusalKind.Constraint, null, "Already completed");
        ProduceRefusal(refusal).ShouldBeNull();
    }

    object? ProduceRefusal(CommandRefusal refusal)
    {
        var branch = typeof(CommandInvocation).GetMethod("FindBranch", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_invocation, [refusal])!;
        var handler = (Func<CommandRefusal, object?>)branch.GetType().GetProperty("Handler")!.GetValue(branch)!;
        return handler(refusal);
    }
}
#endif
