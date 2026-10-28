// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

// Executor/reference parity only: the renderer deliberately retains STAGE-ESM-043 for multiple same-event production
// triggers until Chronicle can return ordered per-trigger batches (Cratis/Chronicle#5248).
public class when_comparing_atomic_reaction_batch_corpora : Specification
{
    [Theory]
    [InlineData("single", false)]
    [InlineData("folder", false)]
    [InlineData("reordered", false)]
    [InlineData("relocated", false)]
    [InlineData("single", true)]
    [InlineData("folder", true)]
    [InlineData("reordered", true)]
    [InlineData("relocated", true)]
    public async Task should_execute_and_match_every_event_specification(string formName, bool legacy)
    {
        var corpus = legacy ? AtomicReactionBatchesCorpus.Legacy : AtomicReactionBatchesCorpus.V22;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(EventSubset(compilation.Value!.Model)).Plan!;
        plan.Specifications.Count.ShouldEqual(corpus.SpecificationExpectations.Length - 2);
        plan.Specifications.ShouldNotBeEmpty();
        foreach (var specification in plan.Specifications.Values)
        {
            var expectation = corpus.SpecificationExpectations.Single(expectation => expectation.Name == specification.Name);
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            int? worldFacts = null;
            var executor = new SemanticSpecificationExecutor();

            // A friend assembly exposes duplicate linked admission types from the renderer and executor.
            typeof(SemanticSpecificationExecutor).GetProperty("ObserveWorldFacts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(executor, (Action<SemanticId, int>)((_, count) => worldFacts = count));
            var result = (await executor.Run(plan, new([specification.Id]), new())).Results.Single();
            Assert.Equal(expectation.WorldFactCount!.Value, worldFacts);
            reference.Passed.ShouldEqual(expectation.Passed);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);

            // Require actual Stage execution, not an unsupported/NotRendered result.
            Assert.True(result.Outcome == (expectation.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed), $"{formName}/{specification.Name}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            result.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
            result.Trace!.Rejection.ShouldEqual(expectation.RejectionMessage);
            if (reference.Execution is SemanticRejected rejected)
            {
                rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                rejected.Details.ShouldEqual(expectation.RejectionMessage);
            }
            else
            {
                var accepted = (SemanticAccepted)reference.Execution;
                Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), result.Trace.Facts.Select(fact => fact.EventContract));
            }
            if (legacy && specification.Name == "ReservationSurvivesChange") reference.Execution.ShouldBeOfExactType<SemanticAccepted>();
        }
    }

    static ExecutableSemanticModel EventSubset(ExecutableSemanticModel model) => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
    {
        Triggers = [],
        Modules = [.. model.Application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Reactions = [.. slice.Reactions.Where(reaction => reaction.Name is not ("Application" or "Clock"))],
                    Specifications = [.. slice.Specifications.Where(specification => specification.WhenTrigger is null && specification.WhenClock is null)]
                })]
            })]
        })]
    });
}
