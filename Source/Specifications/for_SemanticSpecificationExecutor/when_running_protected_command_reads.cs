// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_protected_command_reads : Specification
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public async Task should_match_all_nineteen_reference_outcomes(string formName)
    {
        var corpus = ProtectedCommandReadsCorpus.V25;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        plan.Revision.ShouldEqual(corpus.SemanticRevision);
        var factCounts = new Dictionary<SemanticId, int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => factCounts.Add(id, count) };
        var report = await executor.Run(plan, new([]), new(), TestContext.Current.CancellationToken);
        report.Results.Count.ShouldEqual(19);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            Assert.True(reference.Passed, $"{formName}/{expectation.Name}: {string.Join(';', reference.Failures)}");
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            var result = report.Results.Single(result => result.SpecificationId == expectation.Specification.ToString());
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
                $"{formName}/{expectation.Name}: {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            factCounts[expectation.Specification].ShouldEqual(expectation.WorldFactCount.Value);
            result.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
            result.Trace!.Rejection.ShouldEqual(expectation.RejectionMessage);
            if (reference.Execution is SemanticAccepted accepted)
            {
                var fixture = plan.Specifications[expectation.Specification];
                (fixture.GivenEvents.Length + result.Trace.Facts.Count).ShouldEqual(expectation.WorldFactCount.Value);
                result.Trace.Facts.Count.ShouldEqual(accepted.Facts.Length);
            }
            else
            {
                ((SemanticRejected)reference.Execution).Category.ShouldEqual(expectation.RejectionCategory!.Value);
                result.Trace.Facts.ShouldBeEmpty();
            }
        }
    }
}
