// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_a_wrong_nested_expectation : given.a_structured_event
{
    void Establish()
    {
        Compile(action: "command");
        var specification = _plan.Specifications.Values.Single();
        var contract = _plan.Events.Values.Single();
        var entries = contract.Properties.Single(property => property.Name == "entries").Id;
        var wrong = new SemanticSpecificationEvent(contract.Id, []);
        var values = specification.When!.Values.ToDictionary(member => _plan.Commands.Values.Single().Properties.Single(property => property.Id == member.TargetProperty).Name, member => member.Value);
        var originalEntries = (SemanticArrayValue)values["entries"];
        var firstEntry = (SemanticCompositeValue)originalEntries.Values[0];
        var wrongEntry = firstEntry with { Properties = [firstEntry.Properties.Single() with { Value = SemanticValue.Text("wrong") }] };
        var wrongEntries = originalEntries with { Values = [wrongEntry, .. originalEntries.Values.Skip(1)] };
        wrong = wrong with { Values = [.. contract.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Id == entries ? wrongEntries : values[property.Name]))] };
        typeof(Cratis.Screenplay.Semantics.Execution.SemanticExecutionPlan)
            .GetField("<Specifications>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(_plan, _plan.Specifications.SetItem(specification.Id, specification with { ThenEvents = [wrong] }));
    }
    Task Because() => Execute();
    [Fact] void should_compare_the_structured_fact() => AssertOutcome(SemanticSpecificationOutcome.Failed);
}
