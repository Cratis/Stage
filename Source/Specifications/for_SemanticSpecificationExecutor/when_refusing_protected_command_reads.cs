// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_protected_command_reads : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan()), []).Plan!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_examine_every_specification() => _report.Results.Count.ShouldEqual(13);
    [Fact] void should_never_execute_without_protected_reads() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_protected_read_boundary() => _report.Results.ShouldEachConformTo(result => result.Unsupported!.Details.StartsWith("STAGE-ESM-044:", StringComparison.Ordinal));
}
