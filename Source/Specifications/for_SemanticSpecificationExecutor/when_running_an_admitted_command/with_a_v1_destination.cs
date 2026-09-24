// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_v1_destination : a_command_only_plan
{
    protected override CanonicalCorpusVector Corpus => RegisterProjectCorpus.LegacyV1;
    SemanticSpecificationRun _reference = null!;
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        _reference = new SemanticSpecificationRunner().Run(_plan, _specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new());
    }

    [Fact] void should_match_the_reference_acceptance() => Xunit.Assert.True(_reference.Passed && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed, $"Reference: {_reference.Execution.Kind}; Stage: {_report.Results.Single().Outcome} {string.Join(',', _report.Results.Single().Failures)} {_report.Results.Single().Unsupported?.Details}");
    [Fact] void should_append_the_untyped_destination() => _report.Results.Single().Trace!.Facts.Single().EventSource.ShouldEqual(SemanticRunContext.Canonical(((SemanticAccepted)_reference.Execution).Facts.Single().Destination));
    [Fact] void should_not_invent_a_v2_event_source_type() => _report.Results.Single().Trace!.Facts.Single().EventSourceType.ShouldBeNull();
}
