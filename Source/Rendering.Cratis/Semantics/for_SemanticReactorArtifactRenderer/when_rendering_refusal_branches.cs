// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
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
        source.ShouldContain($".OnRefused(Refused.{expected}, refusal =>");
        source.ShouldContain(".Acknowledge(Refused.Any)");
        source.ShouldContain("CommandRefusalKind.Validation => \"validation\"");
        source.ShouldContain("CommandRefusalKind.Constraint => \"constraint\"");
        source.ShouldContain("CommandRefusalKind.Authorization => \"authorization\"");
        source.ShouldContain("refusal.Message");
        source.ShouldNotContain("ToString()");
        source.ShouldContain("throw new WorkerRefusalKindIsUnknown()");
        source.ShouldContain("class WorkerRefusalKindIsUnknown() : Exception");
        source.ShouldNotContain("InvalidOperationException");
        source.ShouldContain("ExecuteCommandsAsSystem(\"Automation\")");
        if (selector.Contains("constraint", StringComparison.Ordinal)) source.ShouldContain("refusal.ConstraintName");
        source.ShouldNotContain("ConstraintName ??");
    }

    [Fact]
    public void should_spell_no_type_with_global_outside_using_directives()
    {
        SetContext(Compile(RefusalSource));
        var reaction = _located.Slice.Reactions.Single();
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        source.Split('\n').Where(line => !line.TrimStart().StartsWith("using ", StringComparison.Ordinal)).ShouldEachConformTo(line => !line.Contains("global::", StringComparison.Ordinal));
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
        var source = SemanticReactorArtifactRenderer.Render(
            _located,
            reaction with
            {
                Triggers = [trigger with { Invokes = [invocation with { OnRefused = [branch with { Produces = [first, second] }, invocation.OnRefused[1]] }] }]
            },
            _context).Content;
        source.ShouldContain("EventForEventSourceId(context.EventSourceId, new");
        source.ShouldContain("EventForEventSourceId(\"other\", new");
        source.IndexOf("Tags = [\"first\"]", StringComparison.Ordinal).ShouldBeLessThan(source.IndexOf("Tags = [\"second\"]", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("command", true)]
    [InlineData("requirement", true)]
    [InlineData("concept", true)]
    [InlineData("constraint", true)]
    [InlineData("command", false)]
    [InlineData("requirement", false)]
    [InlineData("concept", false)]
    [InlineData("constraint", false)]
    public void should_preserve_verbatim_messages_or_refuse_localized_refusal_payloads(string origin, bool localized)
    {
        var message = localized ? "$strings.failure" : "\"exact\"";
        if (localized && origin == "constraint") message = "\"$strings.failure\"";
        var input = origin switch
        {
            "command" => RefusalSource.Replace("      constraint OneCompletion", $"        validate\n          id not empty message {message}\n      constraint OneCompletion", StringComparison.Ordinal),
            "requirement" => RefusalSource.Replace("      constraint OneCompletion", $"        validate\n          require id != \"\"\n            message {message}\n      constraint OneCompletion", StringComparison.Ordinal),
            "concept" => $"concept Note : String\n  validate\n    not empty message {message}\n" + RefusalSource
                .Replace("      command Complete\n        id String identifier", "      command Complete\n        id String identifier\n        note Note", StringComparison.Ordinal)
                .Replace("            id = id\n            on refused", "            id = id\n            note = \"\"\n            on refused", StringComparison.Ordinal),
            _ => RefusalSource.Replace("        unique event Completed", $"        unique event Completed\n        message {message}", StringComparison.Ordinal)
        };
        var model = Compile(input);
        var profile = CratisRendering.CreateProfile(
            "Reactions",
            new("Reactions", "Reactions"),
            null,
            new Dictionary<string, string> { ["reactions.en.strings"] = "failure = \"Localized prose\"\n" },
            "en");
        var plan = new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        if (localized)
        {
            plan.Artifacts.ShouldBeEmpty();
            plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-039" && diagnostic.Message.Contains("unresolved key", StringComparison.Ordinal));
        }
        else
        {
            Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        }
    }

    [Fact]
    public void should_refuse_ambiguous_first_constraint_names_before_emission()
    {
        SetContext(Compile(RefusalSource.Replace("      reaction Worker", "      constraint AnotherCompletion\n        unique event Completed\n      reaction Worker", StringComparison.Ordinal)));
        Plan();
        _plan.Artifacts.ShouldBeEmpty();
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-039" && diagnostic.Message.Contains("candidate/sorted-name order", StringComparison.Ordinal));
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
