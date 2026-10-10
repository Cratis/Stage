// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_any_order_needing_assignment : a_routed_plan
{
    SemanticSpecificationOutcome _legacy;
    bool _legacyReference;
    bool _orderedFailed;
    bool _impossibleFailed;

    async Task Because()
    {
        var wildcard = _specification.ThenEvents[0] with { Route = null };
        var exact = wildcard with { EventSource = new(Text, SemanticValue.Text("exact")) };
        var specification = _specification with
        {
            When = _specification.When! with { Values = [new(_command.Properties[0].Id, SemanticValue.Text("exact")), new(_command.Properties[1].Id, SemanticValue.Text("other"))] },
            ThenEventsInAnyOrder = true,
            ThenEvents = [wildcard, exact]
        };
        var command = _command with
        {
            Route = null,
            Properties = [.. _command.Properties.Select(property => property with { IsIdentifier = true })],
            Produces = [_command.Produces[0], _command.Produces[0] with { Destination = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _command.Properties[1].Id) }]
        };
        await Run(With(specification, command, legacy: true));
        _legacy = _result.Outcome;
        _legacyReference = _reference.Passed;
        await Run(With(specification with { ThenEventsInAnyOrder = false }, command));
        AssertParity(false);
        _orderedFailed = !_reference.Passed;
        await Run(With(specification with { ThenEvents = [exact, exact] }, command));
        AssertParity(false);
        _impossibleFailed = !_reference.Passed;
        await Run(With(specification, command));
    }

    [Fact] void should_pass_with_assignment_in_v8_like_the_reference() => AssertParity(true);
    [Fact] void should_preserve_the_greedy_failure_in_v7() => _legacy.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_preserve_the_reference_v7_outcome() => _legacyReference.ShouldBeFalse();
    [Fact] void should_keep_ordered_expectations_positional() => _orderedFailed.ShouldBeTrue();
    [Fact] void should_not_assign_the_same_fact_twice() => _impossibleFailed.ShouldBeTrue();
}
