// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_an_unformattable_route_and_an_unallocated_destination : a_routed_plan
{
    bool _expectedRejectionMatched;

    async Task Because()
    {
        var period = _command.Properties.Single(property => property.Name == "period");
        var specification = _specification with
        {
            When = _specification.When! with { Values = [.. _specification.When.Values.Select(value => value.TargetProperty == period.Id ? value with { Value = SemanticValue.Text(string.Empty) } : value)] },
            ThenEvents = [],
            ThenErrors = [new(null, SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.Empty))]
        };
        var command = _command with { Destination = null, Produces = [.. _command.Produces.Select(produced => produced with { Destination = null })] };
        await Run(With(specification, command));
        AssertParity(true);
        _expectedRejectionMatched = _reference.Passed;
        await Run(With(specification with { ThenErrors = [], ThenEvents = _specification.ThenEvents }, command));
    }

    [Fact] void should_report_the_reference_rejection_instead_of_identity_allocation() => _expectedRejectionMatched.ShouldBeTrue();
    [Fact] void should_fail_an_acceptance_expectation_like_the_reference() => AssertParity(false);
    [Fact] void should_reject_without_appending_any_fact() => _result.Trace!.Facts.ShouldBeEmpty();
    [Fact] void should_have_a_contract_rejection_in_the_reference() => ((SemanticRejected)_reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
}
