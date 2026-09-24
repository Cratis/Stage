// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_requirement_warning : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var command = _plan.Commands[_specification.When!.Command];
        var property = command.Properties.First(value => value.Type.Kind == SemanticTypeReferenceKind.Concept &&
            _plan.Model.Application.Concepts.Single(concept => concept.Id == value.Type.Target).Primitive == SemanticPrimitiveType.Text);
        var condition = new SemanticComparison(new(property.Id, null), SemanticComparisonOperator.NotEqual, new(property.Id, null));
        var changedCommand = command with { Requirements = [new SemanticRequirement(condition, "$strings.requirement") { Severity = SemanticValidationSeverity.Warning }] };
        var specification = _specification with { ThenEvents = [], ThenErrors = [new(null, "$strings.requirement")] };
        var plan = WithBehavior(specification, changedCommand);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_reject_with_the_same_string_key_as_the_reference() => Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed && _report.Results.Single().Trace?.Rejection == ((SemanticRejected)_reference.Execution).Details, $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {_report.Results.Single().Unsupported?.Details} {string.Join(',', _report.Results.Single().Failures)}");
    [Fact] void should_append_no_facts() => _report.Results.Single().Trace!.Facts.Count.ShouldEqual(0);
}
