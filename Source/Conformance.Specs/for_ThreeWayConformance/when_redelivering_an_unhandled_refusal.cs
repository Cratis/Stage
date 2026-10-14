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

public class when_redelivering_an_unhandled_refusal : Specification
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
                amount Decimal
                produces Started
                  for id
                  id = id
                  amount = amount
              command Complete
                id String identifier
                amount Decimal
                validate
                  amount > 0 message "Positive amount required"
                produces Completed
                  for id
              event Started
                id String
                amount Decimal
              event Completed
              reaction Worker
                when Started
                  invokes Complete
                    id = id
                    amount = amount
              specification Refused
                given Started
                  for "one"
                  id = "one"
                  amount = -1
                when redelivered Started to Worker
                then error "Positive amount required"
        """)).Plan!;

    async Task Because()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        _stage = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    [Fact] void should_match_then_error_on_the_reference() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_match_then_error_on_the_executor() => _stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_rejection_message() => _stage.Trace!.Rejection.ShouldEqual(((SemanticRejected)_reference.Execution).Details);
    [Fact] void should_not_turn_an_invocation_refusal_into_successful_empty_effects() => _stage.ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
}
#endif
