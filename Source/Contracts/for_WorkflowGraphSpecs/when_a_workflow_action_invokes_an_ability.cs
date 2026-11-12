// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Xunit;

namespace Cratis.Stage.Contracts.Lifecycles.for_WorkflowGraphSpecs;

public class when_a_workflow_action_invokes_an_ability : Specification
{
    WorkflowGraphSpec _graph = null!;

    void Because()
    {
        const string Action = "action ChargeLateFee invokes ChargeFee attempts 3 timeout 30 seconds retry after 5 seconds";
        var source = "system Billing\n  ability Charge\n\n" + a_lifecycle_source.Source.Replace(Action, "action ChargeLateFee invokes Billing.Charge", StringComparison.Ordinal);
        if (source.Contains(Action, StringComparison.Ordinal)) throw new InvalidOperationException("The lifecycle source no longer has the action this specification turns into an ability invocation.");
        _graph = WorkflowGraphSpecs.For(a_lifecycle_source.Plan(source)).Single(graph => graph.Workflow.Name == "Collect");
    }

    [Fact] void should_refuse_the_action_instead_of_failing_to_build_the_graph() => _graph.Refusals.Select(refusal => refusal.Code).ShouldContain(LifecycleCodes.AbilityNode);
    [Fact] void should_name_the_action_that_invokes_it() => _graph.Refusals.Single(refusal => refusal.Code == LifecycleCodes.AbilityNode).Message.ShouldContain("'ChargeLateFee' invokes the ability");
}
