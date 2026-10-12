// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_reaction_identity : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ReactionIdentityCorpus.V10.EsmBytes.AsSpan())).Plan!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());

    [Fact] void should_pass_every_run() => Assert.True(_report.Results.All(result => result.Outcome == SemanticSpecificationOutcome.Passed), string.Join(';', _report.Results.Select(result => $"{result.Name}: {result.Unsupported?.Details} {string.Join(',', result.Failures)}")));
    [Fact] void should_execute_every_scenario() => _report.Results.All(result => result.Trace is not null).ShouldBeTrue();
}
