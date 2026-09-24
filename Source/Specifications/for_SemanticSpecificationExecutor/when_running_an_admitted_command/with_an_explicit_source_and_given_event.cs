// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_explicit_source_and_given_event : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var given = _specification with { GivenEvents = [_specification.ThenEvents.Single()] };
        var plan = With(given);
        _reference = new SemanticSpecificationRunner().Run(plan, given.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([given.Id]), new());
    }

    [Fact] void should_pass_when_the_reference_passes() => Xunit.Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed, $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {_report.Results.Single().Unsupported?.Details} {string.Join(',', _report.Results.Single().Failures)}; expected: {System.Text.Json.JsonSerializer.Serialize(_specification.ThenEvents.Single())}; actual: {System.Text.Json.JsonSerializer.Serialize(_report.Results.Single().Trace?.Facts)}");
    [Fact] void should_produce_only_the_new_fact() => _report.Results.Single().Trace!.Facts.Count.ShouldEqual(1);
    [Fact] void should_match_the_reference_fact_trace()
    {
        var expected = ((SemanticAccepted)_reference.Execution).Facts.Single();
        var actual = _report.Results.Single().Trace!.Facts.Single();
        actual.EventContract.ShouldEqual(expected.EventContract.ToString());
        actual.EventSource.ShouldEqual(SemanticRunContext.Canonical(expected.Destination));
        actual.EventSourceType.ShouldEqual(expected.Context?.EventSource.Type.Target.ToString());
        foreach (var property in expected.Values)
        {
            actual.Values[property.TargetProperty.ToString()].ShouldEqual(SemanticRunContext.Canonical(property.Value));
        }
    }
}
