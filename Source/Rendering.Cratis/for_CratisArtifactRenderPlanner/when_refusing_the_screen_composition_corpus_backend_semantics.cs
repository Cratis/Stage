// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_the_screen_composition_corpus_backend_semantics : Specification
{
    bool _success;
    IReadOnlyList<string> _errors = null!;

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
        _success = compilation.Success;
        _errors = [.. compilation.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")];
    }

    [Fact] void should_preserve_the_backend_semantic_refusal() => _success.ShouldBeFalse();
    [Fact] void should_refuse_the_observable_list_query_shape() => _errors.ShouldContain(_ => _.Contains("PLAY0268", StringComparison.Ordinal) && _.Contains("AllWorkItems", StringComparison.Ordinal));
    [Fact] void should_refuse_the_observable_comments_query_shape() => _errors.ShouldContain(_ => _.Contains("PLAY0268", StringComparison.Ordinal) && _.Contains("CommentsForWorkItem", StringComparison.Ordinal));
}
