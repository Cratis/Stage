// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_command_requirements : given.a_compiled_model_using_2x_constructs
{
    IReadOnlyList<Requirement> _requirements = [];

    void Because() => _requirements = _slice.Command!.Requirements;

    [Fact] void should_carry_every_declared_requirement() => _requirements.Count.ShouldEqual(2);
    [Fact] void should_carry_the_declared_message() => _requirements[0].Message.ShouldEqual("Already started");
    [Fact] void should_leave_an_undeclared_message_unset() => _requirements[1].Message.ShouldBeNull();

    // The same condition tree a 'produces when' guard carries — one condition grammar in the language, one here.
    [Fact] void should_carry_the_condition_as_a_comparison() =>
        _requirements[0].Condition.ShouldBeOfExactType<ProducedEventComparison>();
    [Fact] void should_name_the_state_the_requirement_reads() =>
        ((ProducedEventComparison)_requirements[0].Condition).Property.ShouldEqual("InvoiceScope.isStarted");
    [Fact] void should_carry_the_comparison_operator() =>
        ((ProducedEventComparison)_requirements[0].Condition).Operator.ShouldEqual(ProducedEventComparisonOperator.Equal);
    [Fact] void should_carry_the_compared_value() =>
        ((ProducedEventComparison)_requirements[0].Condition).Value.ShouldEqual("false");
}
