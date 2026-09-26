// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_projected_direct_append : a_command_only_plan
{
    SemanticSpecificationRunRecord _stage = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var fact = _original.ThenEvents.Single();
        var specification = _original with
        {
            When = null,
            WhenAppended = new(fact.EventContract, fact.Values) { EventSource = fact.EventSource ?? _original.When!.EventSource },
            ThenEvents = []
        };
        var plan = WithProjection(specification);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
    }

    [Fact] void should_match_the_reference_after_projecting_the_append() =>
        Assert.True(_reference.Passed && _stage.Outcome == SemanticSpecificationOutcome.Passed,
            $"Reference: {string.Join("; ", _reference.Failures)}; Stage: {_stage.Outcome}/{_stage.Unsupported?.Details} {string.Join("; ", _stage.Failures)}");
    [Fact] void should_expose_projected_query_results() => Assert.NotEmpty(_stage.Trace!.Queries);
}
