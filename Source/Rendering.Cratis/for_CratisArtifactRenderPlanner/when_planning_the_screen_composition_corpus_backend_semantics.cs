// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_the_screen_composition_corpus_backend_semantics : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _stateViewArtifacts = string.Empty;

    void Because()
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            document.Text));
        var compilation = new SemanticModelCompiler().Compile(
            ScreenCompositionCorpus.V1.ApplicationName,
            SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));

        var options = new CratisRenderingOptions("Workspaces", "Workspaces");
        var request = new ArtifactRenderRequest(
            model,
            execution.Plan!,
            CratisRendering.CreateProfile(model.Application.Name, options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _plan = new CratisArtifactRenderPlanner().Plan(request);
        var context = new SemanticApplicationContext(request, options);
        _stateViewArtifacts = string.Join('\n', model.Application.Modules
            .SelectMany(module => module.Features)
            .SelectMany(feature => feature.Slices)
            .Where(slice => slice.Kind == SemanticSliceKind.StateView)
            .Select(slice => SemanticStateViewArtifactRenderer.Render(context.Slice(slice.Id), context))
            .Select(_ => _.Content));
    }

    [Fact] void should_preserve_only_the_specification_generation_refusals() =>
        _plan.Diagnostics.Select(_ => _.Code).ShouldEqual(["STAGE-ESM-011", "STAGE-ESM-011", "STAGE-ESM-011"]);

    [Fact] void should_not_reject_the_observable_query_shapes() => _plan.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-010");
    [Fact] void should_not_reject_the_corpus_projections() => _plan.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-017");
    [Fact] void should_not_reject_the_corpus_command_mappings() => _plan.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-006");
    [Fact] void should_emit_the_observable_work_item_list_query() => _stateViewArtifacts.ShouldContain("AllWorkItems");
    [Fact] void should_emit_the_observable_comments_query() => _stateViewArtifacts.ShouldContain("CommentsForWorkItem");
    [Fact] void should_emit_the_by_parameter_query_shape() => _stateViewArtifacts.ShouldContain("workItemId");
}
