// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_described_command_handlers : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan()), []).Plan!;
    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_read_the_v23_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_refuse_every_specification_before_execution() => _report.Results.Count.ShouldEqual(7);
    [Fact] void should_report_unsupported_not_success() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_command_and_requirement() => _report.Results.ShouldEachConformTo(result => result.Unsupported!.Details.StartsWith("STAGE-IMPL-002:", StringComparison.Ordinal) && result.Unsupported.Details.Contains("ApplyLateFee", StringComparison.Ordinal) && result.Unsupported.Details.Contains(_plan.Commands.Values.Single(command => command.Handler is not null).Handler!.RequirementId, StringComparison.Ordinal));
}
