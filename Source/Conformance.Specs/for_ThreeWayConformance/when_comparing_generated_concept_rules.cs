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
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_generated_concept_rules(when_comparing_generated_concept_rules.context fixture) : IClassFixture<when_comparing_generated_concept_rules.context>
{
    [Fact] void should_preserve_the_proven_corpus_specification_across_four_source_forms() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_have_no_three_way_differences() => Assert.Empty(fixture.Differences);

    [Fact]
    public async Task should_execute_the_failing_pin_as_a_contract_rejection_but_refuse_to_render_it()
    {
        var model = context.WithoutOpaqueRule(SemanticModelSerializer.Deserialize(GeneratedConceptRulesCorpus.PortableV20.EsmBytes.AsSpan()));
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        plan.Specifications.Count.ShouldEqual(2);
        var specification = plan.Specifications.Values.Single(specification => specification.Name == "InvalidInviteFixture");
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        reference.Passed.ShouldBeTrue();
        var rejected = (SemanticRejected)reference.Execution;
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        rejected.World.Facts.ShouldBeEmpty();
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new(), TestContext.Current.CancellationToken)).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        stage.Trace.Rejection.ShouldEqual(rejected.Details);
        stage.Trace.Facts.ShouldBeEmpty();
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GeneratedConceptRules", "GeneratedConceptRules"));
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == specification.Id);
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "GeneratedConceptRules";
        protected override string ProjectFile => "GeneratedConceptRules.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => GeneratedConceptRulesCorpus.PortableV20.SourceForms.Select(form =>
            (form.Name, WithoutOpaqueRule(Compile(GeneratedConceptRulesCorpus.PortableV20, form), renderable: true)));

        internal static ExecutableSemanticModel WithoutOpaqueRule(ExecutableSemanticModel model, bool renderable = false)
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slices = feature.Slices.Where(slice => slice.Name != "Review").Select(slice => renderable && slice.Name == "Invite" ? slice with { Specifications = [] } : slice);

            return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Concepts = [.. model.Application.Concepts.Where(concept => concept.Name != "ReviewedCode")],
                Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
            });
        }

        Task Because()
        {
            AddGeneratedSpecification("GenerationDefectProbe.cs", """
                #if DEBUG
                using Cratis.Arc.Testing.Commands;
                using Xunit;
                namespace GeneratedConceptRules.Probes;
                public class GenerationDefectProbe
                {
                    [Fact]
                    public async System.Threading.Tasks.Task should_report_a_failing_pin_as_an_exception_not_input_validation()
                    {
                        using var scenario = new CommandScenario<global::GeneratedConceptRules.Shop.Customers.Invite.Invite>();
                        scenario.PinGeneratedValue("code", new global::GeneratedConceptRules.Common.InviteCode("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab"));
                        var result = await scenario.Execute(new global::GeneratedConceptRules.Shop.Customers.Invite.Invite());
                        Assert.True(result.HasExceptions);
                        Assert.Empty(result.ValidationResults);
                        Assert.False(result.IsSuccess);
                    }
                }
                #endif
                """);
            return Verify();
        }
    }
}
#endif
