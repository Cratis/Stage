// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_redelivering_a_nonzero_given : a_recovery_plan
{
    void Establish()
    {
        var input = _specification.GivenEvents.Single();
        var property = _plan.Events[input.EventContract].Properties.Single();
        var ignored = input with
        {
            EventSource = new(property.Type, SemanticValue.Text("other-source")),
            Values = [new(property.Id, SemanticValue.Text("ignored"))]
        };
        var selected = input with { EventSource = new(property.Type, SemanticValue.Text("payment-a")) };
        Select(_specification with { GivenEvents = [ignored, selected], WhenRedelivered = _specification.WhenRedelivered! with { Given = 1 } });
    }

    Task Because() => Execute();

    [Fact] void should_match_the_reference() => AssertParity();
    [Fact] void should_only_run_the_named_reaction_and_its_downstream_cascade() => _stage.Trace!.Facts.Select(fact => fact.EventContract).ShouldContainOnly(_specification.ThenEvents.Select(fact => fact.EventContract.ToString()).ToArray());
    [Fact] void should_exclude_both_seeded_inputs_from_the_new_effects() => _stage.Trace!.Facts.Count.ShouldEqual(2);
    [Fact] void should_preserve_each_input_exactly_once_in_reference_history() => _reference.Execution.World.Facts.Count(fact => fact.EventContract == _specification.GivenEvents[1].EventContract).ShouldEqual(2);
    [Fact] void should_preserve_the_nonzero_given_destination_in_the_cascade() => _stage.Trace!.Facts.All(fact => fact.EventSource == "\"payment-a\"").ShouldBeTrue();
    [Fact] void should_not_append_the_redelivered_input_again() => _stage.Trace!.Facts.Any(fact => fact.EventContract == _specification.GivenEvents[1].EventContract.ToString()).ShouldBeFalse();
}
#endif
