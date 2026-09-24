// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_direct_append : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var @event = _specification.ThenEvents.Single();
        var specification = _specification with
        {
            When = null,
            WhenAppended = new SemanticSpecificationAppend(@event.EventContract, @event.Values) { EventSource = @event.EventSource }
        };
        var plan = With(specification);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_pass_as_the_reference_does() => Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed, $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {string.Join(',', _report.Results.Single().Failures)}");
    [Fact] void should_append_the_same_fact_as_the_reference() => _report.Results.Single().Trace!.Facts.Single().EventSource.ShouldEqual(SemanticRunContext.Canonical(((SemanticAccepted)_reference.Execution).Facts.Single().Destination));
}
