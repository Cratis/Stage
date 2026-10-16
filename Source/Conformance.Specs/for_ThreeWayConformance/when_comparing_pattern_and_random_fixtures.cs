// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_pattern_and_random_fixtures(when_comparing_pattern_and_random_fixtures.context fixture) : IClassFixture<when_comparing_pattern_and_random_fixtures.context>
{
    [Fact] void should_execute_the_six_renderable_canonical_fixture_cases_on_all_three_paths() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(6);
    [Fact] void should_have_no_three_way_differences() => Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "PatternRandom";
        protected override string ProjectFile => "PatternRandom.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("generation-fixtures/v21-pattern-random", given.pattern_random_fixtures_corpus.V21)];

        Task Because()
        {
            var model = given.pattern_random_fixtures_corpus.V21;
            var instant = new DateTimeOffset(2024, 2, 29, 22, 30, 0, TimeSpan.Zero);
            var checks = model.Application.Concepts.Where(concept => concept.Generation?.Kind == "pattern").Select(concept =>
            {
                var arguments = SemanticGenerationRendering.UsesContext(concept.Generation)
                    ? "new global::Cratis.Concepts.GenerationContext(new global::System.DateTimeOffset(2024, 2, 29, 22, 30, 0, global::System.TimeSpan.Zero), global::Cratis.Concepts.GenerationContext.DefaultTenant)" : string.Empty;
                var pattern = CSharpCodeBuilder.StringLiteral(SemanticGenerationShapes.Pattern(concept, instant));
                var checksum = concept.Generation!.Checksum is { } algorithm
                    ? $"Xunit.Assert.True(global::Cratis.Concepts.GenerateValue.HasValidChecksum(value, global::Cratis.Concepts.Checksum.{SemanticChecksumRendering.Algorithm(SemanticValue.Text(algorithm))}));" : string.Empty;
                return $"[Xunit.Fact] public void should_sample_{concept.Name}() {{ for (var index = 0; index < 256; index++) {{ var value = global::PatternRandom.Common.{concept.Name}.New({arguments}).Value; Xunit.Assert.Matches({pattern}, value); {checksum} }} }}";
            });
            const string numeric = "[Xunit.Fact] public void should_sample_int64_values() { for (var index = 0; index < 256; index++) { var value = global::PatternRandom.Common.Ticket.New().Value; Xunit.Assert.InRange(value, long.MinValue, long.MaxValue); } }";
            AddGeneratedSpecification("SamplingProbe.cs", $"#if DEBUG\nnamespace PatternRandom.Probes; public class Sampling {{ {string.Join('\n', checks)} {numeric} }}\n#endif\n");
            AddUnpinnedSpecification(model, "NumericFixture", "UnpinnedNumeric");
            AddUnpinnedSpecification(model, "UtcLeapDay", "UnpinnedDate");

            return Verify();
        }

        void AddUnpinnedSpecification(ExecutableSemanticModel model, string originalName, string name)
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var original = slice.Specifications.Single(specification => specification.Name == originalName);
            var unpinned = original with { Name = name, When = original.When! with { GeneratedValues = [] } };
            var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [unpinned] }] }] }]
            });
            var plan = CratisRendering.Plan(changed, SemanticExecutionPlan.Compile(changed).Plan!, new(ArtifactRenderScopeKind.Application, changed.Application.Id), new("PatternRandom", "PatternRandom"));
            Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
            var suffix = name == "UnpinnedNumeric" ? "when_unpinned_numeric.cs" : "when_unpinned_date.cs";
            var artifact = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal));
            AddGeneratedSpecification($"{name}.cs", System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
        }
    }
}
#endif
