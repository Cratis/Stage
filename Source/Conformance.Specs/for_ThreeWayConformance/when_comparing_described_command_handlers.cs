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
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_described_command_handlers : Specification
{
    [Fact]
    public async Task should_match_every_reference_outcome_and_refuse_rendering()
    {
        var corpus = DescribedCommandHandlersCorpus.V23;
        var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model, []).Plan!;
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken);
        report.Results.Count.ShouldEqual(7);
        var differences = new List<string>();
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            var stage = report.Results.Single(result => result.SpecificationId == expectation.Specification.ToString());
            reference.Passed.ShouldEqual(expectation.Passed);
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            var expectedOutcome = expectation.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Unsupported;
            if (stage.Outcome != expectedOutcome)
            {
                differences.Add($"{expectation.Name}: {stage.Outcome} instead of {expectedOutcome}: {string.Join(';', stage.Failures)} {stage.Unsupported?.Details}");
                continue;
            }
            if (expectation.Passed) stage.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
            if (reference.Execution is SemanticUnsupported unsupported)
            {
                unsupported.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
                stage.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.ImplementationBody);
                stage.Unsupported.Details.ShouldEqual($"STAGE-IMPL-002: {unsupported.Details}");
            }
            else if (reference.Execution is SemanticRejected rejected)
            {
                rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                stage.Trace!.Rejection.ShouldEqual(rejected.Details);
            }
            else if (reference.Execution is SemanticAccepted accepted)
            {
                Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
            }
        }
        differences.ShouldBeEmpty();

        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("DescribedHandlers", "DescribedHandlers"));
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-IMPL-002" && diagnostic.Message.Contains("ApplyLateFee", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public void should_preserve_the_released_canonical_forms(string name)
    {
        var corpus = DescribedCommandHandlersCorpus.V23;
        var form = corpus.SourceForms.Single(form => form.Name == name);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        compiled.Value.Model.Revision.ShouldEqual(corpus.SemanticRevision);
        byte[] expected = [.. corpus.EsmBytes];
        byte[] actual = [.. SemanticModelSerializer.Serialize(compiled.Value.Model)];
        Assert.Equal(expected, actual);
    }
}
#endif
