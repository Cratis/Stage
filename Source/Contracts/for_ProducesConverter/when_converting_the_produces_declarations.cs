// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProducesConverter;

public class when_converting_the_produces_declarations : given.a_compiled_model_with_produces
{
    ProducedEvent _unconditional = null!;
    ProducedEvent _conditional = null!;
    ProducedEvent _either = null!;

    void Because()
    {
        _unconditional = _produces[0];
        _conditional = _produces[1];
        _either = _produces[2];
    }

    [Fact] void should_carry_every_produced_event_in_declaration_order() => _produces.Count.ShouldEqual(3);
    [Fact] void should_name_the_unconditional_event() => _unconditional.Event.ShouldEqual("InvoiceRegistered");
    [Fact] void should_leave_the_unconditional_event_unguarded() => _unconditional.When.ShouldBeNull();
    [Fact] void should_carry_the_tag() => _unconditional.Tags.ShouldContainOnly("audit");

    [Fact] void should_source_a_mapped_property_from_the_command() => Property(_unconditional, "invoiceId").Kind.ShouldEqual(ProducedValueKind.CommandProperty);
    [Fact] void should_name_the_command_property_it_maps_from() => Property(_unconditional, "invoiceId").Expression.ShouldEqual("invoiceId");
    [Fact] void should_source_a_constant_as_a_literal() => Property(_unconditional, "status").Kind.ShouldEqual(ProducedValueKind.Literal);
    [Fact] void should_render_a_string_literal_as_json() => Property(_unconditional, "status").Expression.ShouldEqual("\"draft\"");
    [Fact] void should_source_the_context_occurred_as_the_occurred_time() => Property(_unconditional, "registeredAt").Kind.ShouldEqual(ProducedValueKind.Occurred);
    [Fact] void should_source_an_environment_expression_from_the_environment() => Property(_unconditional, "source").Kind.ShouldEqual(ProducedValueKind.Environment);
    [Fact] void should_name_the_environment_variable() => Property(_unconditional, "source").Expression.ShouldEqual("SERVICE_NAME");

    [Fact] void should_guard_the_conditional_event() => _conditional.When.ShouldBeOfExactType<ProducedEventComparison>();
    [Fact] void should_compare_against_the_modeled_property() => Comparison(_conditional).Property.ShouldEqual("isProForma");
    [Fact] void should_use_the_modeled_operator() => Comparison(_conditional).Operator.ShouldEqual(ProducedEventComparisonOperator.Equal);
    [Fact] void should_render_the_compared_constant_as_json() => Comparison(_conditional).Value.ShouldEqual("true");

    [Fact] void should_combine_two_conditions() => _either.When.ShouldBeOfExactType<ProducedEventLogicalCondition>();
    [Fact] void should_use_the_modeled_combinator() => ((ProducedEventLogicalCondition)_either.When!).Operator.ShouldEqual(ProducedEventLogicalOperator.Or);

    static ProducedEventProperty Property(ProducedEvent produced, string property) =>
        produced.Properties.Single(candidate => candidate.Property == property);

    static ProducedEventComparison Comparison(ProducedEvent produced) => (ProducedEventComparison)produced.When!;
}
