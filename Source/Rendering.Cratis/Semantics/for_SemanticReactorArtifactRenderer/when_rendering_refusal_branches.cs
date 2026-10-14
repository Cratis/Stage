// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer;

public class when_rendering_refusal_branches : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    const string RefusalSource = """
        module Work
          feature Processing
            slice Automation Handling
              event Started
                id String
              event Completed
              event Recorded
                reason String
                constraint String
                message String
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              command Complete
                id String identifier
                produces Completed
                  for id
              constraint OneCompletion
                unique event Completed
              reaction Worker
                runs as system role "Automation"
                when Started
                  invokes Complete
                    id = id
                    on refused by constraint OneCompletion
                      produces Recorded
                        reason = $refusal.reason
                        constraint = $refusal.constraint
                        message = $refusal.message
                    on refused
                      acknowledge
        """;

    [Theory]
    [InlineData("on refused", "Any")]
    [InlineData("on refused by validation", "ByValidation")]
    [InlineData("on refused by constraint", "ByAnyConstraint")]
    [InlineData("on refused by constraint OneCompletion", "ByConstraint(\"OneCompletion\")")]
    [InlineData("on refused by authorization", "ByAuthorization")]
    public void should_render_the_released_selector_and_lowercase_reason_mapping(string selector, string expected)
    {
        var input = RefusalSource.Replace("on refused by constraint OneCompletion", selector, StringComparison.Ordinal);
        if (!selector.Contains("constraint", StringComparison.Ordinal)) input = input.Replace("constraint = $refusal.constraint", "constraint = \"none\"", StringComparison.Ordinal);
        SetContext(Compile(input));
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
        var reaction = _located.Slice.Reactions.Single();
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        source.ShouldContain("Invoke.Command(new");
        source.ShouldContain($".OnRefused(global::Cratis.Arc.Chronicle.Reactors.Refused.{expected}, refusal =>");
        source.ShouldContain(".Acknowledge(global::Cratis.Arc.Chronicle.Reactors.Refused.Any)");
        source.ShouldContain("CommandRefusalKind.Validation => \"validation\"");
        source.ShouldContain("CommandRefusalKind.Constraint => \"constraint\"");
        source.ShouldContain("CommandRefusalKind.Authorization => \"authorization\"");
        source.ShouldContain("refusal.Message");
        source.ShouldNotContain("ToString()");
        source.ShouldContain("throw new WorkerRefusalKindIsUnknown()");
        source.ShouldContain("class WorkerRefusalKindIsUnknown() : global::System.Exception");
        source.ShouldNotContain("System.InvalidOperationException");
        source.ShouldContain("ExecuteCommandsAsSystemAttribute(\"Automation\")");
        if (selector.Contains("constraint", StringComparison.Ordinal)) source.ShouldContain("refusal.ConstraintName");
        source.ShouldNotContain("ConstraintName ??");
    }

    [Fact]
    public void should_preserve_order_destinations_and_tags_for_multiple_branch_outputs()
    {
        SetContext(Compile(RefusalSource));
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var invocation = trigger.Invokes.Single();
        var branch = invocation.OnRefused[0];
        var first = branch.Produces.Single() with { Tags = ["first"] };
        var second = first with { Destination = SemanticExpression.FromValue(SemanticValue.Text("other")), DestinationType = _context.Events[trigger.Source].Properties.Single().Type, Tags = ["second"] };
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction with
        {
            Triggers = [trigger with { Invokes = [invocation with { OnRefused = [branch with { Produces = [first, second] }, invocation.OnRefused[1]] }] }]
        }, _context).Content;
        source.ShouldContain("EventForEventSourceId(context.EventSourceId, new");
        source.ShouldContain("EventForEventSourceId(\"other\", new");
        source.IndexOf("Tags = [\"first\"]", StringComparison.Ordinal).ShouldBeLessThan(source.IndexOf("Tags = [\"second\"]", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_flattening_refusal_trigger_boundaries_before_emission()
    {
        SetContext(Compile(RefusalSource));
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        ReplaceAutomation(_located.Slice with { Reactions = [reaction with { Triggers = [trigger, trigger] }] });
        Plan();
        _plan.Success.ShouldBeFalse();
        _plan.Artifacts.ShouldBeEmpty();
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-039" && diagnostic.Message.Contains("trigger subgroup", StringComparison.Ordinal));
    }
}
