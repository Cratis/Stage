// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_unique_event_constraint : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var given = _specification.ThenEvents.Single();
        var constraint = new SemanticConstraint("OnlyOnce", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(given.EventContract, [])], [], false, null);
        var specification = _specification with { GivenEvents = [given], ThenEvents = [], ThenErrors = [new("OnlyOnce", "Constraint 'OnlyOnce' is violated: the event source already has the constrained event.")] };
        var plan = WithBehavior(specification, constraint: constraint);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_match_the_reference_constraint_rejection() => Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed && _report.Results.Single().Trace?.Rejection == ((SemanticRejected)_reference.Execution).Details, $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {_report.Results.Single().Unsupported?.Details} {string.Join(',', _report.Results.Single().Failures)}");
    [Fact] void should_report_the_constraint_name_as_code() => _report.Results.Single().Trace!.RejectionCode.ShouldEqual("OnlyOnce");
    [Fact] void should_append_no_new_fact() => _report.Results.Single().Trace!.Facts.Count.ShouldEqual(0);
}
