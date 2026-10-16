// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_executing_an_unpinned_generation_rule : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunRecord _result = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single(specification => specification.Name == "GeneratorBodyNotExecuted");
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with
            {
                Specifications = [specification with { When = specification.When! with { GeneratedValues = [] } }]
            }] }] }]
        });
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
    }

    async Task Because() => _result = (await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results.Single();

    [Fact] void should_require_a_deterministic_fixture() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_identity_allocation() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.IdentityAllocation);
    [Fact] void should_keep_the_missing_fixture_message() => _result.Unsupported!.Details.ShouldEqual("Generated command values require deterministic fixtures.");
}
