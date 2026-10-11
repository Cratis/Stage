// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_reaction_identity : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ReactionIdentityCorpus.V10.EsmBytes.AsSpan())).Plan!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());

    [Fact] void should_refuse_every_run() => _report.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly([SemanticSpecificationOutcome.Unsupported]);
    [Fact] void should_name_identity_before_generic_reaction_admission() => _report.Results.All(result => result.Unsupported!.Details.StartsWith("STAGE-ESM-034:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_execute_nothing() => _report.Results.All(result => result.Trace is null).ShouldBeTrue();
}
