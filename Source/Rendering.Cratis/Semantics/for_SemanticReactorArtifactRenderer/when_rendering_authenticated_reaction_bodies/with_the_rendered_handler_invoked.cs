// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
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
    public void should_run_the_authored_body_with_the_bound_event_local_and_context(bool file)
    {
        var (_, assembly) = Render(reaction_body_model.Body, file);
        var result = Invoke(assembly, "Broken", null);
        Assert.Equal(["Broken"], Assert.IsType<IEnumerable<object>>(result, exactMatch: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_return_null_without_running_the_body_when_the_guard_is_false(bool file)
    {
        var source = reaction_body_model.Source($"throw new System.InvalidOperationException(\"{Sentinel}\");", file, extraProperty: "        count Int")
            .Replace("      reaction Notifier", "      reaction Notifier\n        where count > 0", StringComparison.Ordinal);
        var loaded = reaction_body_model.LoadSource(source, file ? $"throw new System.InvalidOperationException(\"{Sentinel}\");" : null);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        Assert.Null(Invoke(assembly, "Broken", 0));
        var thrown = Assert.Throws<TargetInvocationException>(() => Invoke(assembly, "Broken", 1));
        Assert.Contains(Sentinel, thrown.InnerException!.Message, StringComparison.Ordinal);
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

    static object? Invoke(Assembly assembly, string title, int? count)
    {
        var type = EventType(assembly);
        var @event = count is null ? Activator.CreateInstance(type, title) : Activator.CreateInstance(type, title, count.Value);
        var reactor = Activator.CreateInstance(assembly.GetTypes().Single(candidate => candidate.Name == "Notifier"));
        var context = EventContext.Empty with { EventSourceId = "one", SequenceNumber = 0UL };
        return Handler(assembly).Invoke(reactor, [@event, context]);
    }
}
#endif
