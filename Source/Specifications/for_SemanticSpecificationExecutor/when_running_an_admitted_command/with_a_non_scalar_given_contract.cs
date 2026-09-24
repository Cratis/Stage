// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_non_scalar_given_contract : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        // Construct a plan whose Given event has an unsupported contract shape. The model validator
        // prevents this shape in the corpus's command, so admission must still guard external plans.
        var given = _specification with { GivenEvents = [_specification.ThenEvents[0]] };
        var plan = With(given);
        var contract = plan.Events[given.GivenEvents[0].EventContract];
        var modified = contract with { Properties = [contract.Properties[0] with { Type = contract.Properties[0].Type with { IsCollection = true } }, .. contract.Properties.Skip(1)] };
        typeof(Cratis.Screenplay.Semantics.Execution.SemanticExecutionPlan)
            .GetField("<Events>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(plan, plan.Events.SetItem(contract.Id, modified));
        _report = await new SemanticSpecificationExecutor().Run(plan, new([given.Id]), new());
    }

    [Fact] void should_return_typed_unsupported_instead_of_a_runtime_failure() => Xunit.Assert.True(_report.Results.Single().Outcome == SemanticSpecificationOutcome.Unsupported && _report.Results.Single().Unsupported?.Capability == StageExecutionCapability.Command);
}
