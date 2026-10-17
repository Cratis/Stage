// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_reach_scoped_bodies : given.a_body_reach_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_run_a_specification_with_unreached_reducer_concept_and_command_bodies() => _report.Results.Single(result => result.Name == "Unrelated").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_refuse_a_reached_reducer_with_its_capability() => _report.Results.Single(result => result.Name == "Feeding").Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_identify_the_reached_reducer_body() => _report.Results.Single(result => result.Name == "Feeding").Unsupported!.Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");

    [Theory]
    [InlineData("given")]
    [InlineData("append")]
    [InlineData("state")]
    [InlineData("absence")]
    [InlineData("query")]
    public void should_refuse_a_reducer_reached_without_a_feeding_command(string reach)
    {
        var specification = _plan.Specifications.Values.Single(specification => specification.Name == "Unrelated") with { When = null, ThenEvents = [] };
        var changed = _plan.Events.Values.Single(@event => @event.Name == "Changed");
        var model = _plan.ReadModels.Values.Single();
        var key = SemanticValue.Text("one");
        specification = reach switch
        {
            "given" => specification with { GivenEvents = [new(changed.Id, []) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), key) }] },
            "append" => specification with { WhenAppended = new(changed.Id, []) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), key) } },
            "state" => specification with { ThenReadModels = [new(model.Id, key, [])] },
            "absence" => specification with { ThenAbsentReadModels = [new(model.Id, key)] },
            _ => specification with { ThenQueries = [new(_plan.Queries.Values.Single().Id, key, [])] }
        };
        var refusal = SemanticRunAdmission.Check(_plan, specification);
        refusal!.Capability.ShouldEqual(StageExecutionCapability.Projection);
        refusal.Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold'");
    }

    [Theory]
    [InlineData("Validate", "Command 'Validate'", "command-body")]
    [InlineData("Review", "Concept 'Reviewed'", "concept-body")]
    [InlineData("ReviewNested", "Concept 'Reviewed'", "concept-body")]
    public void should_refuse_only_the_reached_command_or_concept(string name, string owner, string requirement)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var specification = _plan.Specifications.Values.Single(specification => specification.Name == "Unrelated");
        var refusal = SemanticRunAdmission.Check(_plan, specification with { When = specification.When! with { Command = command.Id } });
        refusal!.Capability.ShouldEqual(StageExecutionCapability.Command);
        refusal.Details.ShouldContain("STAGE-IMPL-003: ");
        refusal.Details.ShouldContain(owner);
        refusal.Details.ShouldContain(requirement);
    }
}
