// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

public class when_running_the_register_project_corpus : Specification
{
    readonly List<string> _failures = [];

    [Fact]
    public void should_match_the_reference_for_admitted_specs_and_block_projected_acceptance() => Assert.True(_failures.Count == 0, string.Join(Environment.NewLine, _failures));

    protected async Task Because()
    {
        foreach (var corpus in new[] { RegisterProjectCorpus.LegacyV1, RegisterProjectCorpus.V2 })
        {
            foreach (var form in corpus.SourceForms)
            {
                var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
                var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                    catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
                var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
                if (!compilation.Success)
                {
                    _failures.Add($"{corpus.Name}/{form.Name}: compilation failed");
                    continue;
                }

                var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
                if (plan.Revision != corpus.SemanticRevision)
                {
                    _failures.Add($"{corpus.Name}/{form.Name}: semantic revision changed");
                }

                foreach (var expectation in corpus.SpecificationExpectations)
                {
                    var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                    var report = await new SemanticSpecificationExecutor().Run(plan, new([expectation.Specification]), new());
                    var result = Assert.Single(report.Results);
                    if (reference.Execution.Kind != expectation.Outcome || !reference.Passed ||
                        (expectation.RejectionMessage is { } referenceMessage && reference.Execution is SemanticRejected rejected && rejected.Details != referenceMessage))
                    {
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: reference did not meet corpus expectation");
                    }
                    if (result.Outcome == SemanticSpecificationOutcome.Passed && !reference.Passed)
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: Stage falsely passed");

                    // An accepted registration feeds the project projection, which is not executed per run; a rejection
                    // appends nothing, so it runs and must agree with the reference.
                    if (expectation.Outcome == SemanticExecutionOutcomeKind.Accepted &&
                        (result.Outcome != SemanticSpecificationOutcome.Unsupported || result.Unsupported?.Capability != StageExecutionCapability.Projection))
                    {
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: expected Unsupported(Projection), got {result.Outcome}/{result.Unsupported?.Capability}");
                    }

                    if (expectation.Outcome == SemanticExecutionOutcomeKind.Rejected && result.Outcome != SemanticSpecificationOutcome.Passed)
                    {
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: expected Passed, got {result.Outcome}/{result.Unsupported?.Capability} {string.Join("; ", result.Failures)}");
                    }

                    if (result.Outcome == SemanticSpecificationOutcome.Passed != reference.Passed && result.Outcome != SemanticSpecificationOutcome.Unsupported)
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: pass parity failed");
                    if (result.Trace?.Rejection is { } rejection && expectation.RejectionMessage is { } message && rejection != message)
                        _failures.Add($"{corpus.Name}/{form.Name}/{expectation.Name}: rejection mismatch");
                }
            }
        }
    }
}
