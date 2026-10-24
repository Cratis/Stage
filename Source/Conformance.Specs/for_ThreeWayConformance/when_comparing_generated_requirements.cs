// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_generated_requirements(when_comparing_generated_requirements.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_generated_requirements.context>
{
    [Fact] void should_execute_all_three_cases_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    [Fact]
    public async Task should_reject_unmet_requirements_without_generated_or_appended_effects()
    {
        var plan = SemanticExecutionPlan.Compile(generated_requirement_cases.Compile()).Plan;
        var specification = plan.Specifications.Values.Single(value => value.Name == "RequirementUnmet");
        specification.When.GeneratedValues.ShouldBeEmpty();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        reference.Passed.ShouldBeTrue();
        var rejection = (SemanticRejected)reference.Execution;
        rejection.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        rejection.World.Facts.ShouldBeEmpty();
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new(), TestContext.Current.CancellationToken)).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        stage.Trace.Rejection.ShouldEqual(rejection.Details);
        stage.Trace.Facts.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_a_hand_built_requirement_reading_a_generated_property()
    {
        var model = generated_requirement_cases.WithGeneratedRead();
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var specification = plan.Specifications.Values.First();
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new(), TestContext.Current.CancellationToken)).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        stage.Unsupported.Details.ShouldContain("STAGE-ESM-038");
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GeneratedRequirements", "GeneratedRequirements"));
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-038" && diagnostic.Artifact == plan.Commands.Values.Single().Id);
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "GeneratedRequirements";
        protected override string ProjectFile => "GeneratedRequirements.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("requirements", generated_requirement_cases.Compile())];

        Task Because()
        {
            // Screenplay v4.180.1 SemanticCommandExecution.Validate checks request values before Generate.
            // Arc v22.104.0 resolves GeneratedValue parameters via CommandHandlerArgumentResolver after validation.
            AddGeneratedSpecification("RequirementOrderProbe.cs", """
                #if DEBUG
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Arc.Testing.Commands;
                using Xunit;
                namespace GeneratedRequirements.Probes;
                public class RequirementOrderProbe
                {
                    [Fact]
                    public async System.Threading.Tasks.Task should_validate_before_resolving_any_generated_value()
                    {
                        using var scenario = new CommandScenario<global::GeneratedRequirements.Orders.Registration.Register.RegisterOrder>();
                        var result = await scenario.Execute(new global::GeneratedRequirements.Orders.Registration.Register.RegisterOrder("one", 0), Xunit.TestContext.Current.CancellationToken);
                        Assert.False(result.IsSuccess);
                        Assert.False(result.HasExceptions);
                        Assert.Contains(result.ValidationResults, failure => failure.Message == "Quantity must be positive");
                        Assert.Empty(scenario.GeneratedValues.All<global::GeneratedRequirements.Common.Receipt>("receipt"));
                        Assert.Empty(scenario.AppendedEvents);
                    }
                }
                #endif
                """);
            return Verify();
        }
    }
}
#endif
