// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_invalid_pattern_fixtures : Specification
{
    [Theory]
    [InlineData("WrongUtcDate")]
    [InlineData("RepeatedDateMismatch")]
    [InlineData("AuthoredChecksumArithmetic")]
    public async Task should_agree_on_rejection_and_refuse_rendering_the_invalid_pin(string name)
    {
        var original = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan());
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single(specification => specification.Name == name);
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, original.Application with
        {
            Concepts = [.. original.Application.Concepts.Where(concept => concept.Generation?.Kind != "implementation")],
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with
                    {
                        Commands = [.. slice.Commands.Where(command => command.Name != "TenantValue")],
                        Specifications = [specification]
                    }]
                }]
            }]
        });
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        reference.Passed.ShouldBeTrue();
        reference.Execution.ShouldBeOfExactType<SemanticRejected>();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("PatternRandom", "PatternRandom"));
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011");
        rendered.Artifacts.ShouldBeEmpty();
    }
}
#endif
