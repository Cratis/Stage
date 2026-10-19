// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_recovery_corpus_specifications : Specification
{
    [Fact]
    public async Task should_compare_unmodified_corpus_including_sourceless_givens_and_render_refusals()
    {
        var corpus = RecoverySpecificationsCorpus.V17;
        var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Specifications.Values.Select(specification => specification.Name).ShouldContainOnly(["RedeliveryCascade", "FilteredRedelivery", "NothingFollowsAppend"]);
        plan.Specifications.Count.ShouldEqual(corpus.SpecificationExpectations.Length);

        // These unmodified members omit given sources; the reference establishes them at the null destination and so does Stage.
        string[] sourcelessGivens = ["RedeliveryCascade", "FilteredRedelivery"];
        plan.Specifications.Values.Where(specification => specification.GivenEvents.Any(given => given.EventSource is null))
            .Select(specification => specification.Name).ShouldContainOnly(sourcelessGivens);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var specification = plan.Specifications[expectation.Specification];
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            Assert.True(reference.Passed, $"{specification.Name}: {string.Join(';', reference.Failures)}");
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{specification.Name}: {stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
            var accepted = (SemanticAccepted)reference.Execution;
            Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
            Assert.Equal(accepted.Facts.Select(fact => fact.Route), stage.Trace.Facts.Select(fact => fact.Route));

            // Every member runs on the reference and has a tested executor outcome and render refusal.
            var selected = a_recovery_plan.WithSpecification(model, specification);
            var rendered = CratisRendering.Plan(selected, SemanticExecutionPlan.Compile(selected).Plan!, new(ArtifactRenderScopeKind.Application, selected.Application.Id), new("Recovery", "Recovery"));
            rendered.Success.ShouldBeFalse();
            rendered.Artifacts.ShouldBeEmpty();
            var refusal = rendered.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == specification.Id);
            refusal.Message.ShouldContain(specification.WhenRedelivered is not null ? "given events cannot be represented faithfully" : "no-observer");
        }
    }

    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public void should_preserve_the_canonical_source_forms(string name)
    {
        var corpus = RecoverySpecificationsCorpus.V17;
        var form = corpus.SourceForms.Single(form => form.Name == name);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        compiled.Value!.Model.Revision.ShouldEqual(corpus.SemanticRevision);
        Assert.Equal([.. corpus.EsmBytes], SemanticModelSerializer.Serialize(compiled.Value.Model).ToArray());
    }
}
#endif
