// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_optional_validated_composite_concepts(when_comparing_optional_validated_composite_concepts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_optional_validated_composite_concepts.context>
{
    [Fact] void should_cover_absent_parents_members_valid_invalid_collections_and_recursion() => fixture.Outcomes.Length.ShouldEqual(16);
    [Fact] void should_match_every_executed_path() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_agree_on_all_composite_inputs() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", outcome, StringComparison.Ordinal));

    [Fact]
    void should_admit_every_renderer_admitted_well_formed_composite_command_shape_without_opaque_rules()
    {
        var model = optional_validated_composite_concept_cases.Admitted();
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("OptionalComposites", "OptionalComposites"));
        Assert.True(rendered.Success, string.Join(';', rendered.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Equal(16, plan.Specifications.Count);
        Assert.All(plan.Specifications.Values, specification => Assert.Null(Semantics.SemanticRunAdmission.Check(plan, specification)));
    }

    [Theory]
    [InlineData("RequiredDetails")]
    [InlineData("OptionalDetails")]
    public async Task should_omit_specifications_with_explicit_null_members_that_both_engines_execute(string shape)
    {
        var model = optional_validated_composite_concept_cases.ExplicitNull(shape);
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([plan.Specifications.Values.Single().Id]), new())).Results);
        Assert.Equal("Passed", stage.Outcome.ToString());
        var rendered = CratisRendering.Plan(
            model,
            plan,
            new(ArtifactRenderScopeKind.Application, model.Application.Id),
            new("OptionalComposites", "OptionalComposites"));
        Assert.True(rendered.Success);
        rendered.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        rendered.Refusals.Single().Artifact.ShouldEqual(plan.Specifications.Values.Single().Id);
        Assert.DoesNotContain(rendered.Artifacts, artifact => artifact.Sources.Contains(plan.Specifications.Values.Single().Id));
        Assert.Equal("STAGE-ESM-011", Assert.Single(rendered.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning)).Code);
    }

    [Fact]
    async Task should_omit_reordered_rejection_inputs_that_pass_the_reference()
    {
        var model = optional_validated_composite_concept_cases.Reordered();
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        Assert.Equal("Label pattern", Assert.IsType<SemanticRejected>(reference.Execution).ValidationFailures[0].Message);
        var rendered = CratisRendering.Plan(
            model,
            plan,
            new(ArtifactRenderScopeKind.Application, model.Application.Id),
            new("OptionalComposites", "OptionalComposites"));
        Assert.True(rendered.Success);
        rendered.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        rendered.Refusals.Single().Artifact.ShouldEqual(specification.Id);
        Assert.DoesNotContain(rendered.Artifacts, artifact => artifact.Sources.Contains(specification.Id));
        Assert.Equal("STAGE-ESM-011", Assert.Single(rendered.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning)).Code);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
        Assert.Equal("Passed", stage.Outcome.ToString());
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "OptionalComposites";
        protected override string ProjectFile => "OptionalComposites.csproj";
        protected override bool RequireStageExecution => true;

        // Let Verify read failing probe facts from the TRX and mark their individual Rendered outcomes Failed.
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", optional_validated_composite_concept_cases.Admitted())];

        async Task Because()
        {
            var rendered = CreatePlan();
            Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
            var model = optional_validated_composite_concept_cases.Admitted();
            var plan = SemanticExecutionPlan.Compile(model).Plan;
            var options = new CratisRenderingOptions(ApplicationName, ApplicationName);
            var request = new ArtifactRenderRequest(model, plan, CratisRendering.CreateProfile(ApplicationName, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
            var application = new SemanticApplicationContext(request, options);
            var types = new SemanticTypeSystem(application);
            var probes = plan.Specifications.Values.Select(specification =>
            {
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(reference.Passed, string.Join(';', reference.Failures));
                var failures = reference.Execution is SemanticRejected rejected
                    ? rejected.ValidationFailures.Select(failure => new[] { failure.Message, failure.Severity.ToString() }).ToArray()
                    : [];
                var invalid = specification.Name.EndsWith("Invalid", StringComparison.Ordinal);
                Assert.Equal(invalid, failures.Length > 0);
                if (invalid)
                {
                    string[] expected = specification.Name.StartsWith("Recursive", StringComparison.Ordinal)
                        ? ["Count minimum", "Count excluded", "Count minimum", "Count excluded"]
                        : [.. Enumerable.Repeat<string[]>(["Count minimum", "Count excluded", "Amount minimum", "Amount excluded", "Label pattern", "Label excluded"], specification.Name.StartsWith("Collection", StringComparison.Ordinal) ? 2 : 1).SelectMany(messages => messages)];
                    Assert.Equal(expected, failures.Select(failure => failure[0]));
                }
                var command = plan.Commands[specification.When.Command];
                var property = command.Properties.Single(property => property.Name == "value");
                var value = specification.When.Values.Single(value => value.TargetProperty == property.Id).Value;
                var behaviorNamespace = SliceNaming.Namespace(ApplicationName, application.DeclaringSlice(specification.Id).Path) +
                    ".when_" + Identifiers.ToSnakeCase(specification.Name);

                // Verify groups TRX facts by this behavior namespace; a mismatch now fails this outcome too.
                return $$"""
                    namespace {{behaviorNamespace}}
                    {
                        public class OrderedCompositeConceptFailures
                        {
                            [Fact]
                            public async Task should_match_reference_messages_severities_and_order()
                            {
                                var commandType = typeof(OptionalComposites.Common.Count).Assembly.GetTypes().Single(type => type.Name == {{JsonSerializer.Serialize(command.Name)}});
                                {{types.Type(property.Type)}} value = {{types.Value(value, property.Type)}};
                                using var scenario = new Cratis.Arc.Testing.Commands.CommandScenario<object>().UseDecisionReads();
                                using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                                var result = await scenario.Execute(commandType.GetConstructors().Single().Invoke(["one", value]));
                                Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                                var expected = System.Text.Json.JsonSerializer.Deserialize<string[][]>({{JsonSerializer.Serialize(JsonSerializer.Serialize(failures))}})!;
                                Assert.Equal(expected.Select(pair => (pair[0], pair[1])), result.ValidationResults.Select(failure => (failure.Message, failure.Severity.ToString())));
                            }
                        }
                    }
                    """;
            });
            AddGeneratedSpecification("OrderedCompositeConceptFailures.cs", $$"""
                #if DEBUG
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Xunit;
                {{string.Join('\n', probes)}}
                #endif
                """);
            await Verify();
        }
    }
}
#endif
