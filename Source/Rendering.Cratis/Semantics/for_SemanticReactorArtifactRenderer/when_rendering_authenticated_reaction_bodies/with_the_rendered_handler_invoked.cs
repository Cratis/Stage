// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_the_rendered_handler_invoked
{
    const string Sentinel = "body ran";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_carry_the_semantic_reactor_identity_and_replay_exclusion(bool file)
    {
        var (loaded, assembly) = Render(reaction_body_model.Body, file);
        var reaction = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        var type = assembly.GetTypes().Single(candidate => candidate.Name == "Notifier");
        Assert.Equal(reaction.Id.ToString(), type.GetCustomAttribute<ReactorAttribute>()!.Id.ToString());
        Assert.NotNull(type.GetCustomAttribute<OnceOnlyAttribute>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_be_a_handler_chronicle_registers(bool file)
    {
        var (_, assembly) = Render(reaction_body_model.Body, file);
        var handler = Handler(assembly);
        Assert.True(handler.IsEventHandlerMethod([EventType(assembly)]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_run_the_authored_body_with_the_bound_event_local_and_context(bool file)
    {
        const string body = "return [new HandlerObserved(Title, @event.Title, context.EventSourceId.ToString(), (int)context.SequenceNumber.Value)];";
        var source = reaction_body_model.Source(body, file).Replace(
            "      reaction Notifier",
            """
                  event HandlerObserved
                    localTitle String
                    eventTitle String
                    sourceId String
                    sequenceNumber Int
                  reaction Notifier
            """,
            StringComparison.Ordinal);
        var loaded = reaction_body_model.LoadSource(source, file ? body : null);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var (invocation, result) = await Invoke(assembly, "Broken");
        Assert.True(invocation.IsSuccess, string.Join("; ", invocation.GetFailureDetails().Messages));
        var observed = Assert.Single(Assert.IsType<IEnumerable<object>>(result, exactMatch: false));
        var type = assembly.GetTypes().Single(candidate => candidate.Name == "HandlerObserved");
        Assert.Equal(type, observed.GetType());
        Assert.Equal("Broken", type.GetProperty("LocalTitle")!.GetValue(observed));
        Assert.Equal("Broken", type.GetProperty("EventTitle")!.GetValue(observed));
        Assert.Equal("one", type.GetProperty("SourceId")!.GetValue(observed));
        Assert.Equal(42, type.GetProperty("SequenceNumber")!.GetValue(observed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_return_null_without_running_the_body_when_the_guard_is_false(bool file)
    {
        var source = reaction_body_model.Source($"throw new System.InvalidOperationException(\"{Sentinel}\");", file, extraProperty: "        count Int")
            .Replace("      reaction Notifier", "      reaction Notifier\n        where count > 0", StringComparison.Ordinal);
        var loaded = reaction_body_model.LoadSource(source, file ? $"throw new System.InvalidOperationException(\"{Sentinel}\");" : null);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var (guarded, result) = await Invoke(assembly, "Broken", 0);
        Assert.True(guarded.IsSuccess, string.Join("; ", guarded.GetFailureDetails().Messages));
        Assert.Null(result);
        var (unguarded, _) = await Invoke(assembly, "Broken", 1);
        Assert.True(unguarded.IsFailed);
        Assert.Contains(Sentinel, string.Join("; ", unguarded.GetFailureDetails().Messages), StringComparison.Ordinal);
    }

    static (LoadedSemanticModel Loaded, Assembly Assembly) Render(string body, bool file)
    {
        var loaded = reaction_body_model.Load(body, file);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        return (loaded, opaque_policy_model.Compile(plan));
    }

    static Type EventType(Assembly assembly) => assembly.GetTypes().Single(candidate => candidate.Name == "TicketOpened");

    static MethodInfo Handler(Assembly assembly) => assembly.GetTypes().Single(candidate => candidate.Name == "Notifier").GetMethod("TicketOpened")!;

    static async Task<(ReactorInvocationResult Invocation, object? ReturnValue)> Invoke(Assembly assembly, params object[] arguments)
    {
        var type = EventType(assembly);
        var @event = Activator.CreateInstance(type, arguments);
        var reactorType = assembly.GetTypes().Single(candidate => candidate.Name == "Notifier");
        await using var reactor = new ActivatedArtifact(Activator.CreateInstance(reactorType)!, reactorType, NullLogger<ActivatedArtifact>.Instance);
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.AllClrTypes.Returns(ImmutableList.Create(type));
        var eventType = type.GetCustomAttribute<EventTypeAttribute>()!;
        eventTypes.GetEventTypeFor(type).Returns(new EventType(eventType.Id, eventType.Generation));
        var effects = new CapturedSideEffects();
        var invoker = new ReactorInvoker(
            eventTypes,
            new ReactorMiddlewares([]),
            reactorType,
            reactor,
            NullLogger<ReactorInvoker>.Instance,
            effects,
            Substitute.For<IEventStore>());
        var context = EventContext.Empty with { EventSourceId = "one", SequenceNumber = 42UL };
        var invocation = await invoker.Invoke(@event!, context);
        return (invocation, effects.ReturnValue);
    }

    // Capture the real invoker's result dispatch without appending to a running kernel.
    sealed class CapturedSideEffects : IReactorSideEffectHandlers
    {
        public object? ReturnValue { get; private set; }

        public bool CanHandle(ReactorContext reactorContext, object value) => true;

        public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) => true;

        Task<Result<ReactorSideEffectFailure>> IReactorSideEffectHandlers.Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
        {
            ReturnValue = value;
            return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
        }
    }
}
#endif
