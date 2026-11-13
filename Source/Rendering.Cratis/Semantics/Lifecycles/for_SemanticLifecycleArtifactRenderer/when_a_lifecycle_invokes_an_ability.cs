// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles.for_SemanticLifecycleArtifactRenderer;

// A model declaring a system is an ESM version the planner refuses as a whole, so the lifecycle admission is asked directly:
// it is the refusal that remains once the planner admits that version.
public class when_a_lifecycle_invokes_an_ability : Specification
{
    IEnumerable<LifecycleRefusal> _refusals = null!;

    IEnumerable<string> Refusals => _refusals.Where(refusal => refusal.Code == "STAGE-LIFECYCLE-010").Select(refusal => refusal.Message);

    void Because()
    {
        const string Action = "action ChargeLateFee invokes ChargeFee attempts 3 timeout 30 seconds retry after 5 seconds";
        const string Entry = "entering invokes ChargeFee";
        var source = "system Billing\n  ability Charge\n    overdue Bool\n\n" + a_lifecycle_source.RenderableSource
            .Replace(Action, "action ChargeLateFee invokes Billing.Charge\n    overdue = overdue", StringComparison.Ordinal)
            .Replace(Entry, "entering invokes Billing.Charge\n      overdue = overdue", StringComparison.Ordinal);
        if (source.Contains(Action, StringComparison.Ordinal) || source.Contains(Entry, StringComparison.Ordinal)) throw new InvalidOperationException("The renderable lifecycle source no longer has the action and the entry this specification turns into ability invocations.");
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile("InvoiceApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _refusals = SemanticLifecycleAdmission.Check(new SemanticApplicationContext(request, options));
    }

    [Fact] void should_name_the_action_that_invokes_it() => Refusals.Any(message => message.Contains("'ChargeLateFee' of workflow 'Collect' invokes the ability 'Charge'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_state_entry_that_invokes_it() => Refusals.Any(message => message.Contains("of state machine 'InvoiceLifecycle' invokes the ability 'Charge'", StringComparison.Ordinal)).ShouldBeTrue();
}
