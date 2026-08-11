// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_where_a_produced_event_lands : given.a_compiled_model_using_2x_constructs
{
    ProducedEvent _own = null!;
    ProducedEvent _elsewhere = null!;

    void Because()
    {
        var produces = _slice.Command!.Produces;
        _own = produces.Single(produced => produced.Event == "InvoiceActivated");
        _elsewhere = produces.Single(produced => produced.Event == "ContractPolicyActivated");
    }

    // An event with no 'for' lands on the command's own event source — the common case, left unstated.
    [Fact] void should_leave_the_event_source_unset_when_none_is_declared() => _own.For.ShouldBeNull();
    [Fact] void should_carry_the_declared_event_source() => _elsewhere.For.ShouldNotBeNull();
    [Fact] void should_resolve_the_event_source_from_a_command_property() =>
        _elsewhere.For!.Kind.ShouldEqual(ProducedValueKind.CommandProperty);
    [Fact] void should_name_the_property_the_event_source_comes_from() =>
        _elsewhere.For!.Expression.ShouldEqual("contractId");
}
