// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_mixed_effects;

public class with_one_trigger : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    const string Mixed = """
        module Work
          feature Processing
            slice Automation FollowUp
              event Started
                id String
              event Recorded
              event Audited
              event Completed
              event Notified
              command Complete
                id String identifier
                produces Completed
                  for id
              command Notify
                id String identifier
                produces Notified
                  for id
              reaction Worker
                when Started
                  produces Recorded
                    for id
                  produces Audited
                    for "audit"
                  invokes Complete
                    id = id
                  invokes Notify
                    id = id
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_compile_one_handler_with_events_before_commands_in_declaration_order(bool atomic)
    {
        SetContext(Compile((atomic ? "reaction batches atomic\n" : string.Empty) + Mixed));
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        var reaction = _located.Slice.Reactions.Single();
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        source.ShouldContain("OnceOnly");
        source.ShouldNotContain("IdempotencyKey");
        var assembly = RenderedOutput.Load(Files());
        var type = assembly.GetType("Reactions.Work.Processing.FollowUp.Worker")!;
        var handler = type.GetMethods().Single(method => method.Name == "Started");
        var started = Activator.CreateInstance(assembly.GetType("Reactions.Work.Processing.FollowUp.Started")!, "one");
        var effects = ((IEnumerable<object>)handler.Invoke(Activator.CreateInstance(type), [started, EventContext.EmptyWithEventSourceId("trigger")])!).ToArray();
        effects.Take(2).Cast<EventForEventSourceId>().Select(effect => effect.Event.GetType().Name).ShouldEqual(["Recorded", "Audited"]);
        effects.Take(2).Cast<EventForEventSourceId>().Select(effect => effect.EventSourceId.Value).ShouldEqual(["one", "audit"]);
        effects.Skip(2).Select(effect => effect.GetType().Name).ShouldEqual(["Complete", "Notify"]);
        effects.Length.ShouldEqual(4);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_render_an_acknowledging_refusal_beside_direct_events(bool atomic)
    {
        var input = Mixed.Replace("          invokes Notify", "            on refused\n              acknowledge\n          invokes Notify", StringComparison.Ordinal);
        SetContext(Compile((atomic ? "reaction batches atomic\n" : string.Empty) + input));
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content.ShouldContain(".Acknowledge(Refused.Any)");
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_direct_events_beside_a_producing_refusal_branch(bool atomic)
    {
        var input = Mixed.Replace("          invokes Notify", "            on refused\n              produces Recorded\n          invokes Notify", StringComparison.Ordinal);
        SetContext(Compile((atomic ? "reaction batches atomic\n" : string.Empty) + input));
        Plan();
        _plan.Success.ShouldBeFalse();
        _plan.Artifacts.ShouldBeEmpty();
        var diagnostic = _plan.Diagnostics.Single(item => item.Code == "STAGE-ESM-037");
        diagnostic.Message.ShouldContain("Worker");
        diagnostic.Message.ShouldContain("Cratis/Arc#3561");
    }

    RenderedFile[] Files() => [.. _plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
