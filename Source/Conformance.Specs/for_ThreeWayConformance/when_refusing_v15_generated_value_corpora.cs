// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

public class when_refusing_v15_generated_value_corpora : Specification
{
    [Fact]
    public async Task should_name_the_remaining_gaps_in_every_source_form()
    {
        var vector = GeneratedValuesCorpus.V15;
        vector.SourceForms.Length.ShouldEqual(4);
        foreach (var form in vector.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var compilation = new SemanticModelCompiler().Compile(vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            compilation.Success.ShouldBeTrue();
            var model = compilation.Value!.Model;
            model.Revision.ShouldEqual(vector.SemanticRevision);
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var expectation in vector.SpecificationExpectations)
            {
                var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                reference.Passed.ShouldBeTrue();
                reference.Execution.Kind.ShouldEqual(expectation.Outcome);
                if (expectation.RejectionMessage is { } message) ((SemanticRejected)reference.Execution).Details.ShouldEqual(message);
            }
            var stage = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
            stage.Results.Count.ShouldEqual(vector.SpecificationExpectations.Length);
            stage.Results.All(result => result.Outcome == SemanticSpecificationOutcome.Unsupported && result.Unsupported!.Details.Contains("conditional", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
            var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GeneratedCorpus", "GeneratedCorpus"));
            rendered.Success.ShouldBeFalse();
            rendered.Artifacts.ShouldBeEmpty();
            rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-004");
            rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
        }
    }
}
