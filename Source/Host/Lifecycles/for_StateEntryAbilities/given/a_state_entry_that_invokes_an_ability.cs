// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Host.Invocations;
using Cratis.Stage.Lifecycles;
using NSubstitute;

namespace Cratis.Stage.Host.Lifecycles.for_StateEntryAbilities.given;

public abstract class a_state_entry_that_invokes_an_ability : Specification
{
    internal StateEntryAbilities _abilities = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected IAbilityInvocations _invocations = null!;
    protected StateEntryInvocation _entry = null!;
    protected ImmutableArray<SemanticPropertyValue> _subject;

    void Establish()
    {
        const string Entry = "entering invokes ChargeFee";
        const string Expectation = "then is Sent\n    then invokes ChargeFee";
        var source = "system Billing\n  ability Charge\n    overdue Bool\n\n" + a_lifecycle_source.Source
            .Replace(Entry, "entering invokes Billing.Charge\n      overdue = overdue", StringComparison.Ordinal)
            .Replace(Expectation, "then is Sent", StringComparison.Ordinal);
        if (source.Contains(Entry, StringComparison.Ordinal)) throw new InvalidOperationException("The lifecycle source no longer has the state entry this specification turns into an ability invocation.");
        _plan = a_lifecycle_source.Plan(source);
        var machine = _plan.Model.Application.StateMachines.Single();
        var effect = machine.States.Single(state => state.Name == "Sent").Entering.Single(entry => entry.Kind == SemanticStateEntryKind.InvokeAbility);
        var overdue = _plan.ReadModels[machine.ReadModel].Properties.Single(property => property.Name == "overdue");
        _subject = [new(overdue.Id, SemanticValue.Boolean(true))];
        _entry = new(machine, "Sent", 1, effect, SemanticValue.Text(a_lifecycle_source.InvoiceId), "entered:InvoiceLifecycle:Sent:4");
        _invocations = Substitute.For<IAbilityInvocations>();
        _abilities = new(_plan, () => _invocations);
    }
}
