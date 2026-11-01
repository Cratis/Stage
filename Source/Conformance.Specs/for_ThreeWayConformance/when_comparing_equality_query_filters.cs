// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_equality_query_filters(when_comparing_equality_query_filters.context fixture) : IClassFixture<when_comparing_equality_query_filters.context>
{
    [Fact] void should_pass_the_correct_reference_expectation() => fixture.ReferencePassed.ShouldBeTrue();
    [Fact] void should_fail_the_wrong_reference_expectation() => fixture.WrongReferencePassed.ShouldBeFalse();
    [Fact] void should_pass_the_stage_filtered_query_assertion() => fixture.StageResults[0].Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_fail_the_stage_wrong_row_assertion() => fixture.StageResults[1].Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_exclude_null_properties_in_the_stage_assertion() => fixture.StageResults[2].Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_build_the_generated_application_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_the_generated_filtered_query_specification() => fixture.CorrectOutcome.ShouldEqual("Passed");
    [Fact] void should_fail_the_generated_wrong_row_specification() => fixture.WrongOutcome.ShouldEqual("Failed");
    [Fact] void should_pass_the_generated_arc_argument_binding_probe() => fixture.ProbeOutcome.ShouldEqual("Passed");
    [Fact] void should_exclude_null_properties_in_the_reference() => fixture.OptionalPropertyReferencePassed.ShouldBeTrue();
    [Fact] void should_exclude_null_properties_in_the_generated_query() => fixture.OptionalPropertyProbeOutcome.ShouldEqual("Passed");

    public class context : a_generated_application
    {
        public bool ReferencePassed { get; private set; }
        public bool WrongReferencePassed { get; private set; }
        public SemanticSpecificationRunRecord[] StageResults { get; private set; } = [];
        public string Warnings { get; private set; } = string.Empty;
        public string CorrectOutcome { get; private set; } = string.Empty;
        public string WrongOutcome { get; private set; } = string.Empty;
        public string ProbeOutcome { get; private set; } = string.Empty;
        public bool OptionalPropertyReferencePassed { get; private set; }
        public string OptionalPropertyProbeOutcome { get; private set; } = string.Empty;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(Model());

        async Task Because()
        {
            var model = Model(includeOptionalPropertySpecification: true);
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single(value => !value.Specifications.IsEmpty);
            var readModel = slice.ReadModels.Single();
            var seeded = slice.Specifications.Select(specification => specification.GivenEvents.IsEmpty ? specification : specification with
            {
                GivenEvents = [],
                GivenReadModels = [.. specification.GivenEvents.Select(fact => new SemanticSpecificationReadModel(
                    readModel.Id,
                    fact.EventSource!.Value,
                    [.. fact.Values.Select(value => new SemanticPropertyValue(readModel.Properties.Single(property => property.Name == ReadModelPropertyName(value.TargetProperty)).Id, value.Value))]))]
            }).ToArray();
            var application = model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [.. seeded] } : value)] }] }]
            };
            var candidate = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
            var plan = SemanticExecutionPlan.Compile(candidate).Plan!;
            ReferencePassed = new SemanticSpecificationRunner().Run(plan, seeded[0].Id).Passed;
            WrongReferencePassed = new SemanticSpecificationRunner().Run(plan, seeded[1].Id).Passed;
            OptionalPropertyReferencePassed = new SemanticSpecificationRunner().Run(plan, seeded[2].Id).Passed;

            // Seed-only models preserve optional-property presence without a concrete Chronicle projection.
            var stageApplication = application with
            {
                Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [.. seeded], Projections = [] } : value)] }] }]
            };
            var stageModel = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, stageApplication);
            var stagePlan = SemanticExecutionPlan.Compile(stageModel).Plan!;
            StageResults = [.. (await new SemanticSpecificationExecutor().Run(stagePlan, new([.. seeded.Select(specification => specification.Id)]), new())).Results];
            AddGeneratedSpecification("QueryFilters.cs", a_query_filter_model.OptionalPropertyProbe);
            Warnings = BuildWarnings(await Run("query-filters-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("query-filters-test.log", true, "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=query-filters.trx", "--results-directory", _evidence.FullName);
            var trx = XDocument.Load(Path.Combine(_evidence.FullName, "query-filters.trx"));
            var results = GeneratedTestResults.Read(trx);
            CorrectOutcome = Outcome(className: "when_filtering_invoices_is_queried");
            WrongOutcome = Outcome(className: "when_filtering_wrong_invoices_is_queried");
            ProbeOutcome = Outcome(methodName: "should_apply_required_optional_and_keyed_filters");
            OptionalPropertyProbeOutcome = Outcome(methodName: "should_exclude_null_properties_from_required_filters");

            string Outcome(string? className = null, string? methodName = null)
            {
                var selected = results.Where(result => result.Matches(className: className, methodName: methodName)).ToArray();
                selected.ShouldNotBeEmpty();

                return selected.All(result => result.Outcome == "Passed") ? "Passed" : "Failed";
            }

            string ReadModelPropertyName(SemanticId propertyId)
            {
                var name = feature.Slices.SelectMany(value => value.Events).Single().Properties.Single(property => property.Id == propertyId).Name;

                return name == "invoiceId" ? "id" : name;
            }
        }

        static ExecutableSemanticModel Model(bool includeOptionalPropertySpecification = false)
        {
            const string specification = """
                      specification FilteringInvoices
                        given InvoiceIssued
                          for "one"
                          invoiceId = "one"
                          status = "open"
                          category = "North"
                          flag = true
                        given InvoiceIssued
                          for "two"
                          invoiceId = "two"
                          status = "closed"
                          category = "North"
                          flag = true
                        given InvoiceIssued
                          for "three"
                          invoiceId = "three"
                          status = "open"
                          category = "South"
                          flag = true
                        then query FilteredInvoices
                          arguments
                            status = "open"
                            category = "North"
                          result
                            id = "one"
                            status = "open"
                            category = "North"
                            flag = true
                """;
            var offset = specification.LastIndexOf("status = \"open\"", StringComparison.Ordinal);
            var wrong = (specification[..offset] + "status = \"closed\"" + specification[(offset + "status = \"open\"".Length)..])
                .Replace("FilteringInvoices", "FilteringWrongInvoices", StringComparison.Ordinal);

            return invoice_model.Compile(a_query_filter_model.OptionalPropertySource + "\n" + specification + "\n" + wrong +
                (includeOptionalPropertySpecification ? "\n" + a_query_filter_model.OptionalPropertySpecification : string.Empty));
        }
    }
}
#endif
