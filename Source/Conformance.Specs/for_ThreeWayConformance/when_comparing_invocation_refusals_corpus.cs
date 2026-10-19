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

public class when_comparing_invocation_refusals_corpus : Specification
{
    [Fact]
    public async Task should_execute_the_unmodified_reference_and_executor_and_refuse_rendered_redelivery()
    {
        var corpus = InvocationRefusalsCorpus.V19;
        var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Specifications.Count.ShouldEqual(corpus.SpecificationExpectations.Length);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var specification = plan.Specifications[expectation.Specification];
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            Assert.True(reference.Passed, string.Join(';', reference.Failures));
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{specification.Name}: {stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
            var accepted = (SemanticAccepted)reference.Execution;
            Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
            var selected = a_recovery_plan.WithSpecification(model, specification);
            var rendered = CratisRendering.Plan(selected, SemanticExecutionPlan.Compile(selected).Plan!, new(ArtifactRenderScopeKind.Application, selected.Application.Id), new("Refusals", "Refusals"));
            rendered.Success.ShouldBeFalse();
            rendered.Artifacts.ShouldBeEmpty();
            rendered.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == specification.Id)
                .Message.ShouldContain("Redelivery with invocation refusal branches requires a context-preserving refusal settlement harness.");
        }
    }

    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public void should_preserve_the_released_canonical_forms(string name)
    {
        var corpus = InvocationRefusalsCorpus.V19;
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
