// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_generated_value_strategies(when_comparing_generated_value_strategies.context fixture) : IClassFixture<when_comparing_generated_value_strategies.context>
{
    [Fact] void should_agree_on_several_initial_facts_and_reused_source() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(7);
    [Fact] void should_have_no_three_way_differences() => Assert.Empty(fixture.Differences);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "GenerationApp";
        protected override string ProjectFile => "GenerationApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("stage-authored-v15", CombinedModel())];

        static ExecutableSemanticModel CombinedModel()
        {
            var strategies = a_generation_model.Model();
            var invites = an_invite_code_model.Model();
            return ExecutableSemanticModel.Create(strategies.LanguageVersion, strategies.SemanticVersion, strategies.Application with
            {
                Concepts = [.. strategies.Application.Concepts, .. invites.Application.Concepts],
                Modules = [.. strategies.Application.Modules, .. invites.Application.Modules]
            });
        }

        Task Because()
        {
            var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
            const string clock = "new global::System.DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, global::System.TimeSpan.Zero)";
            var checks = a_generation_model.Model().Application.Concepts.Select(concept =>
            {
                var timeOrdered = SemanticGenerationRendering.IsTimeOrdered(concept.Generation);
                var arguments = timeOrdered ? $"new global::Cratis.Concepts.GenerationContext({clock}, global::Cratis.Concepts.GenerationContext.DefaultTenant)" : string.Empty;
                var timeCheck = timeOrdered ? $"Xunit.Assert.StartsWith({CSharpCodeBuilder.StringLiteral(SemanticGenerationTime.Prefix(concept, instant))}, value);" : string.Empty;
                return $"[Xunit.Fact] public void should_encode_{concept.Name}() {{ for (var index = 0; index < 256; index++) {{ var value = global::GenerationApp.Common.{concept.Name}.New({arguments}).Value.ToString()!; Xunit.Assert.Matches({CSharpCodeBuilder.StringLiteral(SemanticGenerationShapes.Pattern(concept))}, value); {timeCheck} }} }}";
            });
            var captures = a_generation_model.Model().Application.Concepts.Select(concept =>
            {
                var capture = $"scenario.GeneratedValues.Last<global::GenerationApp.Common.{concept.Name}>({CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(concept.Name))}).Value.ToString()!";
                var timeCheck = SemanticGenerationRendering.IsTimeOrdered(concept.Generation) ? $"Xunit.Assert.StartsWith({CSharpCodeBuilder.StringLiteral(SemanticGenerationTime.Prefix(concept, instant))}, {capture});" : string.Empty;
                return $"Xunit.Assert.Matches({CSharpCodeBuilder.StringLiteral(SemanticGenerationShapes.Pattern(concept))}, {capture}); {timeCheck}";
            });
            var pipeline = $"[Xunit.Fact] public async global::System.Threading.Tasks.Task should_generate_strategy_values_through_arc() {{ await using var scenario = new global::Cratis.Arc.Testing.Commands.CommandScenario<global::GenerationApp.Billing.Creation.Start.Create>(); scenario.Clock({clock}); scenario.AllowGeneration(); var result = await scenario.Execute(new global::GenerationApp.Billing.Creation.Start.Create()); Xunit.Assert.True(result.IsSuccess); {string.Join(' ', captures)} }}";
            AddGeneratedSpecification("EncodingsProbe.cs", $"#if DEBUG\nusing Cratis.Arc.Testing.Commands; namespace GeneratedEncodings; public class Probe {{ {string.Join('\n', checks)} {pipeline} }}\n#endif\n");

            AddUnpinnedCapture();

            return Verify();
        }

        void AddUnpinnedCapture()
        {
            var original = a_generation_model.Model();
            var module = original.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single(slice => slice.Name == "Start");
            var command = slice.Commands.Single();
            var receipt = command.Properties.Single(property => property.Name == "receipt");
            var specification = slice.Specifications.Single(specification => specification.Name == "SeveralInitialFacts");
            var unpinned = specification with
            {
                Name = "CapturingUnpinnedReceipt",
                When = specification.When! with { GeneratedValues = [.. specification.When.GeneratedValues.Where(value => value.TargetProperty != receipt.Id)] }
            };
            var changed = original.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [unpinned] } : value)] }] }]
            };
            var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, changed);
            var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
            Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
            var artifact = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_capturing_unpinned_receipt.cs", StringComparison.Ordinal));
            AddGeneratedSpecification("UnpinnedReceipt.cs", Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
        }
    }
}
#endif
