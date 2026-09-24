// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_mutated_expectation : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var fact = _specification.ThenEvents.Single();
        var value = fact.Values.Single(property => property.Value is SemanticTextValue text && string.Equals(text.Value, "Screenplay", StringComparison.Ordinal));
        var changed = _specification with { ThenEvents = [fact with { Values = [.. fact.Values.Select(property => property.TargetProperty == value.TargetProperty ? property with { Value = SemanticValue.Text("wrong") } : property)] }] };
        var plan = With(changed);
        _reference = new SemanticSpecificationRunner().Run(plan, changed.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([changed.Id]), new());
    }

    [Fact] void should_fail_in_both_engines() => (!_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Failed).ShouldBeTrue();
}
