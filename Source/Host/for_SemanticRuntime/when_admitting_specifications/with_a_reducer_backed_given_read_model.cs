// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_reducer_backed_given_read_model : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    SemanticSpecificationRun _reference = null!;

    void Because()
    {
        const string source = """
            module Projects
              feature Registry
                slice StateChange Registration
                  command Register
                    id String identifier
                    produces Registered
                      for id
                  event Registered
                  event Changed
                  specification SeedingOpaqueState
                    given readmodel Snapshot
                      id = "one"
                      name = "seed"
                    when Register
                      id = "one"
                    then Registered
                      for "one"
                slice StateView View
                  readmodel Snapshot
                    id String key
                    name String
                  reducer Fold => Snapshot
                    on Changed
                      ```csharp
                      return context.State;
                      ```
            """;
        var plan = compiled_plan.From(source);
        _reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        _admission = new(plan);
    }

    [Fact] void should_refuse_the_seeded_specification() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("unsupported");
    [Fact] void should_report_the_projection_capability() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldEqual("Projection");
    [Fact] void should_retain_the_implementation_refusal_code() => _admission.Entries.Single(entry => entry.Kind == "specification").Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold' transition '");
    [Fact] void should_match_the_reference_refusal() => Assert.IsType<SemanticUnsupported>(_reference.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_still_admit_the_unrelated_live_command() => _admission.Entries.Single(entry => entry.Kind == "command").Status.ShouldEqual("supported");
    [Fact] void should_not_block_the_live_model() => _admission.Blocking.ShouldBeEmpty();
}
