// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_guarding_generated_freshness : Specification
{
    const string Fixture = "00112233-4455-4677-8899-aabbccddeeff";
    const string Prior = "        given Created\n          for \"00112233-4455-4677-8899-aabbccddeeff\"";
    const string Accepted = "        then Created\n        then Initialized";
    const string Rejected = "        then error \"A generated event source already has facts; retry the command with a fresh generated value.\"";

    [Theory]
    [InlineData(true, Prior, Rejected, SemanticExecutionOutcomeKind.Rejected)]
    [InlineData(true, "", Accepted, SemanticExecutionOutcomeKind.Accepted)]
    [InlineData(false, Prior, Accepted, SemanticExecutionOutcomeKind.Accepted)]
    public async Task should_agree_with_the_reference_without_guarding_older_versions(bool v15, string prior, string outcome, SemanticExecutionOutcomeKind kind)
    {
        var plan = a_generation_plan.Compile("Uuid", "", Fixture, outcome, prior, v15);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldBeTrue();
        reference.Execution.Kind.ShouldEqual(kind);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        if (kind == SemanticExecutionOutcomeKind.Rejected)
        {
            ((SemanticRejected)reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
            stage.Trace.Rejection.ShouldEqual(((SemanticRejected)reference.Execution).Details);
            stage.Trace.Facts.ShouldBeEmpty();
        }
        else
        {
            stage.Trace.Facts.Count.ShouldEqual(2);
        }
    }
}
