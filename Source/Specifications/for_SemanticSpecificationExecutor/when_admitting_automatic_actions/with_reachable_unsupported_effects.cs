// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_automatic_actions;

public class with_reachable_unsupported_effects : when_settling_reactions.given.a_reaction_plan
{
    [Theory]
    [InlineData(false, "command")]
    [InlineData(true, "command")]
    [InlineData(false, "reducer")]
    [InlineData(true, "reducer")]
    [InlineData(false, "projection")]
    [InlineData(true, "projection")]
    public void should_preserve_admission_for_effects_reached_only_from_automatic_actions(bool clock, string body)
    {
        Compile($"reaction Worker\n  {(clock ? "every 1 hour" : "when Startup")}\n    invokes Complete\n      item = \"one\"", "then Completed", new(Action: clock
            ? "given clock \"2026-10-05T00:00:00Z\"\nwhen clock \"2026-10-05T01:00:00Z\"" : "when trigger Startup"));
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var completed = slice.Events.Single(contract => contract.Name == "Completed");
        var literal = new SemanticValueExpression(SemanticValue.Text("one"));
        var projection = slice.Projections.Single();
        var changed = slice with
        {
            Commands = [.. slice.Commands.Select(command => command.Name == "Complete" && body == "command" ? command with { CodeValidations = [new("automatic-command-body")] } : command)],
            Reducers = body == "reducer" ? [new("Fold", slice.ReadModels.Single().Id, [new(completed.Id, "automatic-reducer-body")])] : [],
            Projections = body switch
            {
                "reducer" => [],
                "projection" => [projection with
                {
                    Scope = null,
                    Transitions = [new(completed.Id, new(AffectedInstanceCardinality.One, literal), [new(slice.ReadModels.Single().Properties.Single().Id, literal)])]
                }],
                _ => slice.Projections
            }
        };
        application = application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application)).Plan!;
        var refusal = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single());
        refusal.ShouldNotBeNull();
        refusal!.Capability.ShouldEqual(body == "command" ? StageExecutionCapability.Command : StageExecutionCapability.Projection);
        if (body != "projection") refusal.Details.ShouldContain("STAGE-IMPL-003:");
    }
}
