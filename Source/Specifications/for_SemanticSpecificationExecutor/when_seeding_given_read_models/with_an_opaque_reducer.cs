// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_an_opaque_reducer : Specification
{
    SemanticSpecificationRunRecord _result = null!;
    SemanticSpecificationRunRecord _unseeded = null!;
    SemanticSpecificationRun _reference = null!;
    SemanticId _readModel;

    async Task Because()
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
                  specification UnrelatedWithoutSeed
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
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OpaqueSeed"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("OpaqueSeed", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _readModel = plan.ReadModels.Values.Single().Id;
        _reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "SeedingOpaqueState").Id);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
        _result = report.Results.Single(result => result.Name == "SeedingOpaqueState");
        _unseeded = report.Results.Single(result => result.Name == "UnrelatedWithoutSeed");
    }

    [Fact] void should_refuse_the_seeded_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_identify_the_seeded_read_model() => _result.Unsupported!.Construct.ShouldEqual(_readModel.ToString());
    [Fact] void should_retain_the_implementation_refusal_code() => _result.Unsupported!.Details.ShouldContain("STAGE-IMPL-003: Reducer 'Fold' transition '");
    [Fact] void should_match_the_reference_refusal() => Assert.IsType<SemanticUnsupported>(_reference.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_not_pass_in_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_still_run_the_unrelated_unseeded_specification() => _unseeded.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
