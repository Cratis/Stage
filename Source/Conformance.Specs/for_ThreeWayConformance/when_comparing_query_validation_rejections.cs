// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_query_validation_rejections(when_comparing_query_validation_rejections.context fixture) : IClassFixture<when_comparing_query_validation_rejections.context>
{
    [Fact] void should_execute_six_reference_specifications() => fixture.Reference.Length.ShouldEqual(6);
    [Fact] void should_match_the_reference_expectations() => fixture.Reference.All(result => result.Passed).ShouldBeTrue();
    [Fact] void should_execute_six_stage_specifications() => fixture.Stage.Length.ShouldEqual(6);
    [Fact] void should_match_the_stage_expectations() => fixture.Stage.All(result => result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_refuse_the_projectionless_corpus_as_an_application() => fixture.CorpusRendering.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-007");
    [Fact] void should_omit_the_refused_corpus_owners() => rendering_refusals.Rejected(fixture.CorpusRendering, "STAGE-ESM-007");
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_all_six_generated_query_specifications() => fixture.GeneratedSpecifications.ShouldEqual(6);
    [Fact] void should_pass_every_generated_assertion() => fixture.GeneratedOutcomes.All(outcome => outcome == "Passed").ShouldBeTrue();

    public class context : a_generated_application
    {
        public SemanticSpecificationRun[] Reference { get; private set; } = [];
        public SemanticSpecificationRunRecord[] Stage { get; private set; } = [];
        public string Warnings { get; private set; } = string.Empty;
        public int GeneratedSpecifications { get; private set; }
        public string[] GeneratedOutcomes { get; private set; } = [];

        public ArtifactRenderPlan CorpusRendering { get; private set; } = null!;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(a_projected_query_validation_model.Compile());

        async Task Because()
        {
            var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(QueryValidationCorpus.V18.EsmBytes.AsSpan())).Plan!;
            CorpusRendering = invoice_model.Plan(plan.Model);
            Reference = [.. plan.Specifications.Keys.Select(id => new SemanticSpecificationRunner().Run(plan, id))];
            Stage = [.. (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results];
            Warnings = BuildWarnings(await Run("query-validation-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("query-validation-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=query-validation.trx", "--results-directory", _evidence.FullName);
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "query-validation.trx")).Descendants(ns + "UnitTestResult").ToArray();
            GeneratedOutcomes = [.. results.Select(result => (string)result.Attribute("outcome")!)];
            GeneratedSpecifications = results.Select(result => ((string)result.Attribute("testName")!).Split(".should_", StringSplitOptions.None)[0]).Distinct().Count();
        }
    }
}
#endif
