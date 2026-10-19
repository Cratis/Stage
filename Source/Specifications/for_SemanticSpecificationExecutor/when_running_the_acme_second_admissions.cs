// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_the_acme_second_admissions : Specification
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public async Task should_match_both_reference_outcomes(string formName)
    {
        var corpus = AcmeSecondAdmissionsCorpus.V30;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        model.LanguageVersion.ShouldEqual(LanguageVersion.V30);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V30);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Revision.ShouldEqual(corpus.SemanticRevision);
        var counts = new Dictionary<SemanticId, int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => counts.Add(id, count) };
        var report = await executor.Run(plan, new([]), new());
        report.Results.Count.ShouldEqual(2);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            Assert.True(reference.Passed, $"{formName}/{expectation.Name}: {string.Join(';', reference.Failures)}");
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            var result = report.Results.Single(result => result.SpecificationId == expectation.Specification.ToString());
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
                $"{formName}/{expectation.Name}: {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            counts[expectation.Specification].ShouldEqual(expectation.WorldFactCount.Value);
            result.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
            result.Trace!.Rejection.ShouldEqual(expectation.RejectionMessage);
            if (reference.Execution is SemanticRejected rejected)
            {
                rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                result.Trace.Facts.ShouldBeEmpty();
            }
        }
    }
}
