// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_an_unformattable_route : a_routed_plan
{
    async Task Because()
    {
        var period = _command.Properties.Single(property => property.Name == "period");
        var specification = _specification with
        {
            When = _specification.When! with { Values = [.. _specification.When.Values.Select(value => value.TargetProperty == period.Id ? value with { Value = SemanticValue.Text(string.Empty) } : value)] },
            ThenEvents = [],
            ThenErrors = [new(null, SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.Empty))]
        };
        await Run(With(specification));
    }

    [Fact] void should_match_the_reference_contract_rejection() => AssertParity(true);
    [Fact] void should_reject_before_appending_any_fact() => _result.Trace!.Facts.ShouldBeEmpty();
    [Fact] void should_have_a_contract_rejection_in_the_reference() => ((SemanticRejected)_reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
}
