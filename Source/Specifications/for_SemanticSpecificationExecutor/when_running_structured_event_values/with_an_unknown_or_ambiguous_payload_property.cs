// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_an_unknown_or_ambiguous_payload_property : given.a_structured_event
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_the_named_property_without_throwing(bool duplicate)
    {
        Compile();
        var specification = _plan.Specifications.Values.Single();
        var fact = specification.GivenEvents.Single();
        var contract = _plan.Events[fact.EventContract];
        var property = contract.Properties[0];
        if (duplicate)
        {
            typeof(SemanticExecutionPlan).GetField("<Events>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(_plan, _plan.Events.SetItem(contract.Id, contract with { Properties = [.. contract.Properties, property] }));
        }
        else
        {
            fact = fact with { Values = [fact.Values[0] with { TargetProperty = contract.Id }, .. fact.Values.Skip(1)] };
            specification = specification with { GivenEvents = [fact] };
        }
        var refusal = SemanticRunAdmission.Check(_plan, specification);
        Assert.NotNull(refusal);
        refusal.Capability.ShouldEqual(StageExecutionCapability.Command);
        refusal.Details.ShouldEqual($"Event '{contract.Name}' payload property '{(duplicate ? property.Id : contract.Id)}' must identify exactly one declared property.");
    }
}
