// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_projecting_redelivery_cascades : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRun _reference = null!;
    SemanticSpecificationRunRecord _stage = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(a_recovery_plan.Compile("""
        module Work
          feature Items
            slice Automation Recovery
              event Started
                id String
              event Followed
                id String
              event Completed
                id String
              reaction Worker
                when Started
                  produces Followed
                    for id
                    id = id
              reaction Finisher
                when Followed
                  produces Completed
                    for id
                    id = id
              readmodel Marker
                id String key
              projection Markers => Marker
                from Completed key id
                  id = id
              specification Materialized
                given Started
                  for "one"
                  id = "one"
                when redelivered Started to Worker
                then Followed
                  for "one"
                  id = "one"
                then Completed
                  for "one"
                  id = "one"
                then readmodel Marker for "one"
                  id = "one"
        """)).Plan!;

    async Task Because()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        _stage = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    [Fact] void should_materialize_the_reference_cascade() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_admit_and_materialize_the_executor_cascade() => _stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_include_the_downstream_projection_in_the_trace() => _stage.Trace!.ReadModels.Count.ShouldEqual(1);
}
#endif
