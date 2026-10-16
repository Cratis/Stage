// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_executing_a_pinned_generation_rule : Specification
{
    SemanticSpecificationRun _reference = null!;
    SemanticSpecificationRunRecord _stage = null!;

    async Task Because()
    {
        var corpus = GenerationFixturesCorpus.V21;
        var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var expected = corpus.SpecificationExpectations.Single(expectation => expectation.Name == "GeneratorBodyNotExecuted");
        _reference = new SemanticSpecificationRunner().Run(plan, expected.Specification);
        _stage = (await new SemanticSpecificationExecutor().Run(plan, new([expected.Specification]), new())).Results.Single();
    }

    [Fact] void should_match_the_frozen_reference_acceptance() => _reference.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_pass_the_reference_specification_without_running_the_body() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_pass_the_same_specification_in_stage() => _stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_refuse_identity_allocation_for_a_pin() => _stage.Unsupported.ShouldBeNull();
}
#endif
