// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_an_unrelated_slice_of_a_v9_model : Specification
{
    ExecutableSemanticModel _v9 = null!;
    ExecutableSemanticModel _v8 = null!;
    ArtifactRenderScope _scope = null!;
    ArtifactRenderPlan _actual = null!;
    ArtifactRenderPlan _baseline = null!;

    void Establish()
    {
        var corpus = PublicEventsCorpus.V9;
        var form = corpus.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var original = form.Documents.Single();
        var source = original.Text.Replace("module Integration", "eventsource Orders\n    identifier OrderId\n    stream Updates\nmodule Integration", StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(original.StableKey), original.StableKey, original.DisplayPath, source);
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _v9 = compilation.Value!.Model;
        var module = _v9.Application.Modules.Single();
        var feature = module.Features.Single();
        var unrelated = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        _scope = new(ArtifactRenderScopeKind.Slice, unrelated.Id);
        _v8 = ExecutableSemanticModel.Create(LanguageVersion.V8, SemanticVersion.V8, _v9.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [unrelated] }] }]
        });
    }

    void Because()
    {
        _baseline = Plan(_v8);
        _actual = Plan(_v9);
    }

    [Fact] void should_render_the_v8_baseline() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_render_despite_the_v9_siblings() => _actual.Success.ShouldBeTrue();
    [Fact] void should_emit_identical_artifact_bytes() => _actual.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)).ShouldContainOnly(_baseline.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    [Fact] void should_emit_nonempty_output() => _actual.Artifacts.ShouldNotBeEmpty();

    ArtifactRenderPlan Plan(ExecutableSemanticModel model) => CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, _scope, new("Contracts", "Contracts"));
}
