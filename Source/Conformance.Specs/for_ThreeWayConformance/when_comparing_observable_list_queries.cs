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

public class when_comparing_observable_list_queries(when_comparing_observable_list_queries.context fixture) : IClassFixture<when_comparing_observable_list_queries.context>
{
    [Fact] void should_pass_each_correct_reference_expectation() => fixture.ReferenceCorrect.ShouldContainOnly(true, true, true, true);
    [Fact] void should_fail_each_wrong_reference_expectation() => fixture.ReferenceWrong.ShouldContainOnly(false, false, false, false);
    [Theory]
    [InlineData("CorrectFilteredById")]
    [InlineData("CorrectFilteredInvoices")]
    void should_pass_each_correct_stage_filtered_expectation(string name) => fixture.StageResults.Single(result => result.Name == name).Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Theory]
    [InlineData("WrongFilteredById")]
    [InlineData("WrongFilteredInvoices")]
    void should_fail_each_wrong_stage_filtered_expectation(string name) => fixture.StageResults.Single(result => result.Name == name).Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Theory]
    [InlineData("CorrectAllInvoices")]
    [InlineData("WrongAllInvoices")]
    [InlineData("CorrectSnapshotInvoices")]
    [InlineData("WrongSnapshotInvoices")]
    void should_refuse_each_unfiltered_collection_with_the_exact_query_gap(string name)
    {
        var result = fixture.StageResults.Single(result => result.Name == name);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Query);
        result.Unsupported.Details.ShouldEqual("Only optional snapshot queries by read-model identifier are admitted.");
    }
    [Fact] void should_compare_all_eight_stage_expectations() => fixture.StageResults.Length.ShouldEqual(8);
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_each_correct_generated_expectation() => fixture.GeneratedCorrect.ShouldContainOnly("Passed", "Passed", "Passed", "Passed");
    [Fact] void should_fail_each_wrong_generated_expectation() => fixture.GeneratedWrong.ShouldContainOnly("Failed", "Failed", "Failed", "Failed");
    [Fact] void should_compare_all_four_query_shapes() => fixture.GeneratedCorrect.Length.ShouldEqual(4);
    [Fact]
    void should_fail_wrong_generated_rows_as_assertions()
    {
        fixture.WrongFailures.Length.ShouldEqual(4);
        fixture.WrongFailures.All(message => message == "Expected: \"closed\"\nActual:   \"open\"").ShouldBeTrue();
        fixture.WrongFailureStacks.All(stack => stack.Contains("Cratis.Specifications.SpecificationAssert.Equal", StringComparison.Ordinal)).ShouldBeTrue();
    }
    [Fact] void should_push_filtered_updates_in_process() => fixture.PushOutcome.ShouldEqual("Passed");

    public class context : a_generated_application
    {
        public bool[] ReferenceCorrect { get; private set; } = [];
        public bool[] ReferenceWrong { get; private set; } = [];
        public SemanticSpecificationRunRecord[] StageResults { get; private set; } = [];
        public string Warnings { get; private set; } = string.Empty;
        public string[] GeneratedCorrect { get; private set; } = [];
        public string[] GeneratedWrong { get; private set; } = [];
        public string[] WrongFailures { get; private set; } = [];
        public string[] WrongFailureStacks { get; private set; } = [];
        public string PushOutcome { get; private set; } = string.Empty;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(Model());

        async Task Because()
        {
            var model = Model();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            var specifications = plan.Specifications.Values.ToArray();
            var runner = new SemanticSpecificationRunner();
            ReferenceCorrect = [.. specifications.Where(specification => specification.Name.StartsWith("Correct", StringComparison.Ordinal)).Select(specification => runner.Run(plan, specification.Id).Passed)];
            ReferenceWrong = [.. specifications.Where(specification => specification.Name.StartsWith("Wrong", StringComparison.Ordinal)).Select(specification => runner.Run(plan, specification.Id).Passed)];
            StageResults = [.. (await new SemanticSpecificationExecutor().Run(plan, new([.. specifications.Select(specification => specification.Id)]), new())).Results];
            AddGeneratedSpecification("ObservablePush.cs", PushProbe);
            Warnings = BuildWarnings(await Run("observable-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("observable-test.log", true, "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=observable.trx", "--results-directory", _evidence.FullName);
            var trx = XDocument.Load(Path.Combine(_evidence.FullName, "observable.trx"));
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var results = trx.Descendants(ns + "UnitTestResult").ToArray();
            GeneratedCorrect = Outcomes(results, "when_correct");
            GeneratedWrong = Outcomes(results, "when_wrong");
            WrongFailures = [.. results.Where(result => ((string)result.Attribute("testName")!).Contains("when_wrong", StringComparison.Ordinal) && (string)result.Attribute("outcome")! == "Failed").Select(result => result.Descendants(ns + "Message").Single().Value)];
            WrongFailureStacks = [.. results.Where(result => ((string)result.Attribute("testName")!).Contains("when_wrong", StringComparison.Ordinal) && (string)result.Attribute("outcome")! == "Failed").Select(result => result.Descendants(ns + "StackTrace").Single().Value)];
            PushOutcome = Outcomes(results, "should_push_matching_rows_and_exclude_other_rows").Single();
        }

        static string[] Outcomes(IEnumerable<XElement> results, string name) =>
            [.. results.Where(result => ((string)result.Attribute("testName")!).Contains(name, StringComparison.Ordinal))
                .GroupBy(result => ((string)result.Attribute("testName")!)[.. ((string)result.Attribute("testName")!).LastIndexOf('.')])
                .Select(group => group.All(result => (string)result.Attribute("outcome")! == "Passed") ? "Passed" : "Failed")];

        static ExecutableSemanticModel Model()
        {
            var source = a_query_filter_model.Source.Replace("      projection InvoiceSummaryProjection", "      query SnapshotInvoices => InvoiceSummary[]\n      projection InvoiceSummaryProjection", StringComparison.Ordinal);
            foreach (var query in new[] { "AllInvoices", "FilteredById", "FilteredInvoices", "SnapshotInvoices" })
            {
                var arguments = query switch
                {
                    "FilteredById" => "          arguments\n            invoiceId = \"one\"\n            status = \"open\"\n            category = \"North\"\n",
                    "FilteredInvoices" => "          arguments\n            status = \"open\"\n            category = \"North\"\n",
                    _ => string.Empty
                };
                var correct = $$"""
                          specification Correct{{query}}
                            given InvoiceIssued
                              for "one"
                              invoiceId = "one"
                              status = "open"
                              category = "North"
                            then query {{query}}
                    {{arguments}}          result
                                invoiceId = "one"
                                status = "open"
                                category = "North"
                    """;
                var offset = correct.LastIndexOf("status = \"open\"", StringComparison.Ordinal);
                var wrong = (correct[..offset] + "status = \"closed\"" + correct[(offset + "status = \"open\"".Length)..])
                    .Replace("Correct", "Wrong", StringComparison.Ordinal);
                source += "\n" + correct + "\n" + wrong;
            }
            return invoice_model.Compile(source);
        }
    }

    const string PushProbe = """
        using System.Reactive.Linq;
        using System.Threading.Channels;
        using Cratis.Chronicle.Testing.Events;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Billing.Invoicing.Lookup;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.Probes;

        public class observable_push
        {
            [Fact]
            public async Task should_push_matching_rows_and_exclude_other_rows()
            {
                using var store = new EventStoreForTesting();
                var emissions = Channel.CreateUnbounded<InvoiceSummary[]>();
                var subject = InvoiceSummary.FilteredInvoices(store.ReadModels, "open", "North");
                using var subscription = subject.Subscribe(rows => emissions.Writer.TryWrite(rows.ToArray()));
                Assert.Empty(await emissions.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                await store.EventLog.Append("one", new InvoiceIssued(InvoiceId: new InvoiceId("one"), Status: "open", Category: new Category("North")));
                var matching = await emissions.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal("one", Assert.Single(matching).InvoiceId.Value);
                await store.EventLog.Append("two", new InvoiceIssued(InvoiceId: new InvoiceId("two"), Status: "closed", Category: new Category("South")));
                var filtered = await InvoiceSummary.FilteredInvoices(store.ReadModels, "open", "North").FirstAsync();
                Assert.Equal("one", Assert.Single(filtered).InvoiceId.Value);
            }
        }
        """;
}
#endif
