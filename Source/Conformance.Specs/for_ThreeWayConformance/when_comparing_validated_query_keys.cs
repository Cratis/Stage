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
    [Fact] void should_accept_valid_and_out_of_rule_keys_in_the_reference() => Assert.All(fixture.ReferenceResults, passed => passed.ShouldBeTrue());
    [Fact] void should_accept_valid_and_out_of_rule_keys_in_stage() => Assert.All(fixture.StageResults, outcome => outcome.ShouldEqual(SemanticSpecificationOutcome.Passed));
    [Fact] void should_match_lookup_and_collection_results_in_the_rendered_query_pipeline() => fixture.RenderedResults.ShouldContain("Passed!");
    [Fact] void should_build_the_rendered_application_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);

    public class context : a_generated_application
    {
        public bool[] ReferenceResults { get; private set; } = [];
        public SemanticSpecificationOutcome[] StageResults { get; private set; } = [];
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
            var stages = new List<SemanticSpecificationOutcome>();
            foreach (var query in slice.Queries)
            {
                foreach (var key in new[] { "invoice-one", "x" })
                {
                    var specification = original with
                    {
                        ThenQueries = [expected with { Query = query.Id, Key = SemanticValue.Text(key), Results = key == "x" ? [] : expected.Results }]
                    };
                    var application = model.Application with
                    {
                        Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [specification] } : value)] }] }]
                    };
                    var candidate = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
                    var plan = SemanticExecutionPlan.Compile(candidate).Plan!;
                    references.Add(new SemanticSpecificationRunner().Run(plan, specification.Id).Passed);
                    stages.Add((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single().Outcome);
                }
            }

            ReferenceResults = [.. references];
            StageResults = [.. stages];
            // Empty query expectations are outside companion-spec admission today. Exercise those
            // vectors through the real generated Arc pipeline without widening that admission here.
            AddGeneratedSpecification("QueryKeys.cs", a_validated_query_key_model.Probe + "\n" + """
                public class validated_keys
                {
                    [Fact]
                    public async Task should_preserve_all_four_query_answers()
                    {
                        var results = await QueryKeys.Execute();
                        Assert.All(results, result => Assert.Empty(result.ValidationResults));
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
