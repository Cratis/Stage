// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_filtered_query_assertions : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Because()
    {
        _plan = compiled_plan.From("""
            module Projects
              feature Registry
                slice StateView Browse
                  command Register
                    id String identifier
                    name String
                    produces Registered
                      for id
                      name = name
                  event Registered
                    name String
                  readmodel Summary
                    id String key
                    name String
                  projection Summarize => Summary
                    from Registered
                      id = $eventSourceId
                      name = name
                  query Filtered => Summary[]
                    filter name String optional
                  specification Matching
                    given Registered
                      for "one"
                      name = "Shared"
                    then query Filtered
                      arguments
                        name = "Shared"
                      result
                        id = "one"
                  specification Seeded
                    given readmodel Summary
                      id = "two"
                      name = "Seeded"
                    then query Filtered
                      result
                        id = "two"
                  specification NotMatching
                    given Registered
                      for "one"
                      name = "Shared"
                    then query Filtered
                      arguments
                        name = "Other"
            """);
        _admission = new(_plan);
    }

    [Fact] void should_admit_every_filtered_specification() => _admission.Entries.Where(entry => entry.Kind == "specification").Select(entry => entry.Status).ShouldContainOnly("supported", "supported", "supported");
    [Fact] void should_share_executor_admission() => _plan.Specifications.Values.ShouldEachConformTo(specification => SemanticRunAdmission.Check(_plan, specification) is null);
    [Fact] void should_match_reference_execution() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
    [Fact] void should_not_block_the_host() => _admission.Blocking.ShouldBeEmpty();
}
