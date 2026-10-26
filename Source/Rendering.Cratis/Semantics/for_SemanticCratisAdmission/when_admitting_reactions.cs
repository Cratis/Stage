// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_reactions : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    [Theory]
    [InlineData("missing")]
    [InlineData("type")]
    [InlineData("foreign-condition-property")]
    [InlineData("text-ordering")]
    public void should_refuse_unfaithful_reactions_without_artifacts(string form)
    {
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        var trigger = reaction.Triggers.Single();
        var produced = trigger.Produces.Single();
        var text = _context.Events[trigger.Source].Properties.Single(property => property.Name == "id");
        var completed = _context.Events.Values.Single(@event => @event.Name == "Completed");
        var target = completed.Properties.Single();
        trigger = form switch
        {
            "missing" => trigger with { Produces = [produced with { Mappings = [] }] },
            "type" => trigger with { Produces = [produced with { EventContract = completed.Id, Mappings = [new(target.Id, SemanticExpression.Property(SemanticExpressionRootKind.Event, text.Id))] }] },
            "text-ordering" => trigger with { Where = new SemanticComparison(new(text.Id, null), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Text("a"))) },
            _ => trigger with { Where = new SemanticComparison(new(_located.Slice.Commands.Single().Properties.Single(property => property.Name == "note").Id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("a"))) }
        };
        var slice = _located.Slice with { Reactions = [reaction with { Triggers = [trigger] }] };
        var diagnostics = SemanticCratisAdmission.Evaluate(_context, [_located with { Slice = slice }]);
        diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-039");
    }

    [Fact]
    public void should_refuse_mixed_effects_across_triggers_on_one_event()
    {
        var production = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        var invocation = _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive").Triggers.Single();
        ReplaceAutomation(_located.Slice with { Reactions = [production with { Triggers = [production.Triggers.Single(), invocation] }] });
        Plan();
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-037"]);
        _plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    public void should_omit_command_specifications_that_cascade_into_reactions()
    {
        const string specification = """
                  specification Starting
                    when StartWork
                      id = "one"
                      amount = 5
                    then Started
                      id = "one"
                      amount = 5
            """;

        // A specification runs only its own slice's reactions (Screenplay decision 0115), so the command and the specification
        // move into the slice that reacts.
        var oneSlice = Source.Replace("    slice Automation FollowUp\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateChange Start\n", "    slice Automation FollowUp\n", StringComparison.Ordinal);
        SetContext(Compile(oneSlice + "\n" + specification));
        Plan();
        _plan.Success.ShouldBeTrue();
        _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning);
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-007");
        _plan.Refusals.Single().Artifact.ShouldEqual(_located.Slice.Specifications.Single().Id);
        _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_located.Slice.Specifications.Single().Id);
    }

    [Fact]
    public void should_include_reactor_names_in_type_collision_admission()
    {
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        ReplaceAutomation(_located.Slice with { Reactions = [reaction with { Name = "Completed" }] });
        Plan();
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-012");
        _plan.Artifacts.ShouldBeEmpty();
    }
}
