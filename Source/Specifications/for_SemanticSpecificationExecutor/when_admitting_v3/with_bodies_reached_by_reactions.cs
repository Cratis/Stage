// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_bodies_reached_by_reactions : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then Completed\n  for \"one\"");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_a_body_reached_only_through_an_invoked_command(bool reducer)
    {
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var completed = slice.Events.Single(@event => @event.Name == "Completed");
        var changed = slice with
        {
            Projections = reducer ? [] : slice.Projections,
            Reducers = reducer ? [new("Fold", slice.ReadModels.Single().Id, [new(completed.Id, "cascade-reducer-body")])] : [],
            Commands = [.. slice.Commands.Select(command => command.Name == "Complete" && !reducer ? command with { CodeValidations = [new("cascade-command-body")] } : command)]
        };
        application = application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application)).Plan!;
        var refusal = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single());
        refusal!.Capability.ShouldEqual(reducer ? StageExecutionCapability.Projection : StageExecutionCapability.Command);
        refusal.Details.ShouldContain("STAGE-IMPL-003: ");
        refusal.Details.ShouldContain(reducer ? "cascade-reducer-body" : "cascade-command-body");
    }
}
