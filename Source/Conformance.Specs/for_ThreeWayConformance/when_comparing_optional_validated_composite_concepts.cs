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
    [Fact] void should_cover_absent_parents_members_valid_invalid_collections_and_recursion() => fixture.Outcomes.Length.ShouldEqual(18);
    [Fact] void should_match_every_executed_path() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pin_the_stage_executor_gap_tracked_in_441() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Unsupported(Command), Rendered=Passed", outcome, StringComparison.Ordinal));

    [Fact]
    async Task should_refuse_reordered_rejection_inputs_that_pass_the_reference()
    {
        var model = optional_validated_composite_concept_cases.Reordered();
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        Assert.Equal("Label pattern", Assert.IsType<SemanticRejected>(reference.Execution).ValidationFailures[0].Message);
        var rendered = CratisRendering.Plan(
            model,
            plan,
            new(ArtifactRenderScopeKind.Application, model.Application.Id),
            new("OptionalComposites", "OptionalComposites"));
        Assert.False(rendered.Success);
        Assert.Empty(rendered.Artifacts);
        Assert.Equal("STAGE-ESM-011", Assert.Single(rendered.Diagnostics).Code);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
        Assert.Equal("Unsupported", stage.Outcome.ToString());
        Assert.Equal("Command", stage.Unsupported?.Capability.ToString());
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "OptionalComposites";
        protected override string ProjectFile => "OptionalComposites.csproj";
        protected override bool RequireStageExecution => false;

        // Let Verify read failing probe facts from the TRX and mark their individual Rendered outcomes Failed.
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", optional_validated_composite_concept_cases.Compile())];

        async Task Because()
        {
            var rendered = CreatePlan();
            Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
            var model = optional_validated_composite_concept_cases.Compile();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
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
                var command = plan.Commands[specification.When!.Command];
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
                                using var scenario = new Cratis.Arc.Testing.Commands.CommandScenario<object>();
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
                using Xunit;
                {{string.Join('\n', probes)}}
                #endif
                """);
            await Verify();
        }
    }
}
#endif
