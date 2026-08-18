// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Reactions;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// An Automation slice arrived carrying its events and nothing that reacts to them, so the whole point of the
/// slice was missing from the model while the slice itself was present.
/// </summary>
public class when_inspecting_the_reactions : given.a_compiled_model_using_previously_dropped_constructs
{
    ReactionDefinition _reaction = null!;

    void Because() => _reaction = _chaseOverdueInvoices.Reactions.Single();

    [Fact] void should_name_the_reaction() => _reaction.Name.ShouldEqual("OverdueChaser");
    [Fact] void should_carry_the_description() =>
        _reaction.Description.ShouldEqual("Reminds the billing contact while an invoice is late");
    [Fact] void should_carry_every_trigger() => _reaction.Triggers.Count.ShouldEqual(3);

    [Fact] void should_carry_the_time_a_scheduled_trigger_runs_at() =>
        Schedule.Time.ShouldEqual(new TimeOnly(8, 0));
    [Fact] void should_leave_an_unqualified_schedule_on_every_day() => Schedule.DayOfWeek.ShouldBeNull();
    [Fact] void should_carry_what_a_scheduled_trigger_appends() =>
        _reaction.Triggers[0].Produces.Single().Event.ShouldEqual("InvoiceMarkedOverdue");

    [Fact] void should_carry_how_often_an_interval_trigger_runs() => Interval.Amount.ShouldEqual(15);
    [Fact] void should_carry_the_unit_an_interval_is_counted_in() => Interval.Unit.ShouldEqual(IntervalUnit.Minutes);

    [Fact] void should_name_what_an_event_trigger_responds_to() => Named.Name.ShouldEqual("InvoiceRegistered");
    [Fact] void should_carry_the_values_a_trigger_takes_from_the_occurrence() =>
        _reaction.Triggers[2].Data.ShouldContainOnly(["invoiceId"]);
    [Fact] void should_carry_the_description_of_a_single_trigger() =>
        _reaction.Triggers[2].Description.ShouldEqual("Re-checks whether a late payment has since arrived");
    [Fact] void should_point_at_the_file_a_trigger_is_implemented_in() =>
        _reaction.Triggers[2].File.ShouldEqual("Reactions/Chase.cs");
    [Fact] void should_carry_the_command_a_trigger_invokes() =>
        _reaction.Triggers[2].Invokes.Single().Command.ShouldEqual("CancelInvoice");
    [Fact] void should_carry_how_an_invoked_command_is_filled() =>
        _reaction.Triggers[2].Invokes.Single().Mappings.Single().Property.ShouldEqual("invoiceId");

    [Fact] void should_narrow_which_occurrences_run_the_reaction() =>
        ((ProducedEventComparison)_reaction.Where!).Property.ShouldEqual("invoiceId");
    [Fact] void should_leave_a_trigger_with_no_body_stating_nothing() =>
        _reaction.Triggers[1].Produces.ShouldBeEmpty();
    [Fact] void should_point_at_no_file_for_a_trigger_with_none() => _reaction.Triggers[1].File.ShouldBeEmpty();

    [Fact] void should_derive_identifiers_deterministically() =>
        EventModelLoader.LoadFromSource(Source)
            .Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "ChaseOverdueInvoices")
            .Reactions.Single().Id.ShouldEqual(_reaction.Id);

    ScheduleTriggerSource Schedule => (ScheduleTriggerSource)_reaction.Triggers[0].Source;

    IntervalTriggerSource Interval => (IntervalTriggerSource)_reaction.Triggers[1].Source;

    NamedTriggerSource Named => (NamedTriggerSource)_reaction.Triggers[2].Source;
}
