// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_validated_query_keys(when_comparing_validated_query_keys.context fixture) : IClassFixture<when_comparing_validated_query_keys.context>
{
    [Fact] void should_accept_valid_keys_and_reject_out_of_rule_keys_in_the_reference() => Assert.All(fixture.ReferenceResults, passed => passed.ShouldBeTrue());
    [Fact] void should_report_the_stage_executor_given_read_model_gap() => Assert.All(fixture.StageResults, result => result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported));
    [Fact] void should_name_the_unsupported_stage_capability() => Assert.All(fixture.StageResults, result => result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.GivenReadModel));
    [Fact] void should_explain_the_stage_seeding_limit() => Assert.All(fixture.StageResults, result => result.Unsupported!.Details.ShouldEqual("Given read-model state cannot be seeded into the per-run projection scenario."));
    [Fact] void should_report_the_same_stage_limit_without_validation_rules() => Assert.Equal(fixture.StageResults.Select(result => (result.Outcome, result.Unsupported)), fixture.StageResultsWithoutValidation.Select(result => (result.Outcome, result.Unsupported)));
    [Fact] void should_match_lookup_and_collection_results_in_the_rendered_query_pipeline() => fixture.RenderedResults.ShouldContain("Passed!");
    [Fact] void should_build_the_rendered_application_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);

    public class context : a_generated_application
    {
        public bool[] ReferenceResults { get; private set; } = [];
        public SemanticSpecificationRunRecord[] StageResults { get; private set; } = [];
        public SemanticSpecificationRunRecord[] StageResultsWithoutValidation { get; private set; } = [];
        public string RenderedResults { get; private set; } = string.Empty;
        public string DebugWarnings { get; private set; } = string.Empty;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_validated_query_key_model.Source));

        async Task Because()
        {
            var model = invoice_model.Compile(a_validated_query_key_model.Source + "\n" + """
                      specification LookingUpInvoice
                        given readmodel InvoiceSummary
                          invoiceId = "invoice-one"
                          description = "North"
                        then query InvoiceById
                          arguments
                            invoiceId = "invoice-one"
                          result
                            invoiceId = "invoice-one"
                            description = "North"
                """);
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single(value => !value.Specifications.IsEmpty);
            var original = slice.Specifications.Single();
            var expected = original.ThenQueries.Single();
            var references = new List<bool>();
            var stages = new List<SemanticSpecificationRunRecord>();
            var stagesWithoutValidation = new List<SemanticSpecificationRunRecord>();
            foreach (var query in slice.Queries)
            {
                foreach (var key in new[] { "invoice-one", "x" })
                {
                    var specification = original with
                    {
                        ThenQueries = [expected with { Query = query.Id, Key = SemanticValue.Text(key), Results = key == "x" ? [] : expected.Results }],
                        ThenErrors = key == "x" ? [new(null, null)] : []
                    };
                    var application = model.Application with
                    {
                        Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [specification] } : value)] }] }]
                    };
                    var languageVersion = key == "x" ? LanguageVersion.V18 : model.LanguageVersion;
                    var semanticVersion = key == "x" ? SemanticVersion.V18 : model.SemanticVersion;
                    var candidate = ExecutableSemanticModel.Create(languageVersion, semanticVersion, application);
                    var plan = SemanticExecutionPlan.Compile(candidate).Plan!;
                    references.Add(new SemanticSpecificationRunner().Run(plan, specification.Id).Passed);

                    // Stage cannot seed Given read-model state into its per-run projection scenario,
                    // regardless of concept validation rules; retain the typed refusal for this leg.
                    stages.Add((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single());
                    var withoutValidation = ExecutableSemanticModel.Create(languageVersion, semanticVersion, application with
                    {
                        Concepts = [.. application.Concepts.Select(concept => concept with { Validations = [] })]
                    });
                    var planWithoutValidation = SemanticExecutionPlan.Compile(withoutValidation).Plan!;
                    stagesWithoutValidation.Add((await new SemanticSpecificationExecutor().Run(planWithoutValidation, new([specification.Id]), new())).Results.Single());
                }
            }

            ReferenceResults = [.. references];
            StageResults = [.. stages];
            StageResultsWithoutValidation = [.. stagesWithoutValidation];

            // Keep coverage of projected and pre-seeded reads alongside the empty-world v18 corpus.
            AddGeneratedSpecification("QueryKeys.cs", a_validated_query_key_model.Probe + "\n" + """
                public class validated_keys
                {
                    [Fact]
                    public async Task should_preserve_all_four_query_answers()
                    {
                        var results = await QueryKeys.Execute();
                        Assert.False(results[1].IsValid);
                        Assert.False(results[3].IsValid);
                        Assert.NotEmpty(results[1].ValidationResults);
                        Assert.NotEmpty(results[3].ValidationResults);
                        Assert.All(results, result => Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages)));
                    }
                }
                """);
            DebugWarnings = BuildWarnings(await Run("validated-query-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            RenderedResults = await Run("validated-query-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
        }
    }
}
#endif
