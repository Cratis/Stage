// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_redelivery_does_not_settle : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRun _reference = null!;
    SemanticSpecificationRunRecord _stage = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(a_recovery_plan.Compile("""
        module Work
          feature Items
            slice Automation Recovery
              command Begin
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
              reaction Worker
                when Started
                  produces Started
                    for id
                    id = id
              specification NeverSettles
                given Started
                  for "one"
                  id = "one"
                when redelivered Started to Worker
                then no events
        """)).Plan!;

    async Task Because()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        _stage = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    [Fact] void should_not_pass_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_match_the_reference_unsupported_outcome() => _reference.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_pass_the_executor() => _stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_reaction_budget() => _stage.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Reaction);
}
#endif
