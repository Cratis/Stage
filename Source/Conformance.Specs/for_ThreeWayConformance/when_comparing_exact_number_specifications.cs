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
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_exact_number_corpus_specifications : Specification
{
    readonly List<ArtifactRenderPlan> _rendering = [];
    readonly List<SemanticSpecificationRun> _reference = [];
    readonly List<SemanticSpecificationRunRecord> _stage = [];
    readonly List<bool> _canonicalMatches = [];
    readonly List<bool> _roundTrips = [];

    async Task Because()
    {
        var corpus = NumberLiteralsCorpus.ExactLiteralsV11;
        foreach (var form in corpus.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            var model = compilation.Value!.Model;
            model.LanguageVersion.ShouldEqual(LanguageVersion.V11);
            model.SemanticVersion.ShouldEqual(SemanticVersion.V11);
            model.Revision.ShouldEqual(corpus.SemanticRevision);
            var bytes = SemanticModelSerializer.Serialize(model);
            _canonicalMatches.Add(bytes.SequenceEqual(corpus.EsmBytes));
            var restored = SemanticModelSerializer.Deserialize(bytes);
            _roundTrips.Add(SemanticModelSerializer.Serialize(restored).SequenceEqual(bytes));
            var plan = SemanticExecutionPlan.Compile(restored).Plan!;
            plan.Specifications.Count.ShouldEqual(corpus.SpecificationExpectations.Length);
            _rendering.Add(invoice_model.Plan(restored));
            foreach (var specification in plan.Specifications.Values)
            {
                var expected = corpus.SpecificationExpectations.Single(expectation => expectation.Specification == specification.Id);
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                reference.Execution.Kind.ShouldEqual(expected.Outcome);
                _reference.Add(reference);
            }
            _stage.AddRange((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        }
    }

    [Fact] void should_preserve_canonical_bytes_in_all_four_source_forms() => _canonicalMatches.ShouldContainOnly(true, true, true, true);
    [Fact] void should_strictly_round_trip_all_four_v11_models() => _roundTrips.ShouldContainOnly(true, true, true, true);
    [Fact] void should_execute_all_eight_reference_cases() => _reference.Count.ShouldEqual(8);
    [Fact] void should_execute_all_eight_stage_cases() => _stage.Count.ShouldEqual(8);
    [Fact] void should_pass_every_reference_case() => _reference.TrueForAll(result => result.Passed && result.Execution.Kind == SemanticExecutionOutcomeKind.Accepted).ShouldBeTrue();
    [Fact] void should_pass_every_stage_case() => _stage.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Passed);
    [Fact] void should_refuse_the_multi_command_slice_in_all_four_forms() => _rendering.Count.ShouldEqual(4);
    [Fact] void should_report_the_existing_slice_capability_refusal() => _rendering.SelectMany(plan => plan.Diagnostics).Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly("STAGE-ESM-004");
    [Fact] void should_emit_no_partial_artifacts() => _rendering.SelectMany(plan => plan.Artifacts).ShouldBeEmpty();
}

public class when_comparing_exact_number_projection_specifications(when_comparing_exact_number_projection_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_exact_number_projection_specifications.context>
{
    [Fact] void should_compare_the_full_precision_command_event_and_read_model() => fixture.Outcomes.Length.ShouldEqual(1);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Invoices";
        protected override string ProjectFile => "InvoiceApp.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("exact-number-projection", invoice_model.Compile(an_exact_number_model.ExecutionSource))];
        Task Because() => Verify();
    }
}
#endif
