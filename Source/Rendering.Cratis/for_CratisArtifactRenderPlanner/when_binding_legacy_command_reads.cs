// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_legacy_command_reads : Specification
{
    public static TheoryData<string> Forms => [.. ProtectedCommandReadsCorpus.Legacy.Select(vector => vector.Name)];

    [Theory]
    [MemberData(nameof(Forms))]
    public void should_keep_legacy_reads_refused_by_the_binder(string name)
    {
        var vector = ProtectedCommandReadsCorpus.Legacy.Single(vector => vector.Name == name);
        var form = vector.SourceForm;
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = SemanticDocumentSet.Create([.. form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text))], catalog);
        var result = new SemanticModelCompiler().Compile(vector.ApplicationName, documents);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0271");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0786");
    }

    [Fact] void should_cover_every_legacy_form() => Forms.Count.ShouldEqual(4);
}
