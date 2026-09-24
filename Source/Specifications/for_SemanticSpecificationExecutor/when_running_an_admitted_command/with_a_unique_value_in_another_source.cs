// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_unique_value_in_another_source : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var @event = _specification.ThenEvents.Single();
        var contract = _plan.Events[@event.EventContract];
        var property = @event.Values.First(value => value.Value is SemanticTextValue && !contract.Properties.Single(candidate => candidate.Id == value.TargetProperty).IsIdentifier);
        var source = @event.EventSource!;
        var given = @event with
        {
            EventSource = new(source.Type, SemanticValue.Text("6ca6599c-eefd-42a1-8e97-3aac1899487e")),
            Values = [.. @event.Values.Select(value => value.TargetProperty == property.TargetProperty && value.Value is SemanticTextValue text
                ? value with { Value = SemanticValue.Text(text.Value.ToLowerInvariant()) } : value)]
        };
        var constraint = new SemanticConstraint("UniqueName", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(@event.EventContract, [property.TargetProperty])], [], true, null);
        var specification = _specification with
        {
            GivenEvents = [given],
            ThenEvents = [],
            ThenErrors = [new("UniqueName", "Constraint 'UniqueName' is violated: another event source already holds the constrained value.")]
        };
        var plan = WithBehavior(specification, constraint: constraint);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_reject_with_the_same_code_and_message() => Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed && _report.Results.Single().Trace?.Rejection == ((SemanticRejected)_reference.Execution).Details, $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {_report.Results.Single().Unsupported?.Details} {string.Join(',', _report.Results.Single().Failures)}");
}
