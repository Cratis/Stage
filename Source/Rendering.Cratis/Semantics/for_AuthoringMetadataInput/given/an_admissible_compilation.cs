// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput.given;

public class an_admissible_compilation : Specification
{
    protected AdmissibleCompilationResult _admissible = null!;
    protected SemanticCompilation _compilation = null!;

    void Establish()
    {
        var source = when_rendering_authoring_documentation.Source.TrimEnd() + "\n" + """
                slice StateView Removed
                  readmodel RemovedView
                    description "Excluded view"
                    id String
                  query Scoped => RemovedView[]
                    description "Excluded query"
                    scoped to identity
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("documentation"), "documentation", "Projects.play", source);
        _admissible = new SemanticModelCompiler().CompileAdmissible("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(_admissible.Compilation.Success, string.Join(Environment.NewLine, _admissible.Compilation.Diagnostics));
        Assert.NotEmpty(_admissible.Exclusions);
        _compilation = _admissible.Compilation.Value!;
    }

    protected static SemanticDocumentSet ReplaceSource(SemanticCompilation compilation, string previous, string replacement) =>
        SemanticDocumentSet.Create(
            [.. compilation.Documents.Documents.Select(document => SemanticSourceDocument.Create(
                document.Id,
                document.StableKey,
                document.DisplayPath,
                document.Text.Replace(previous, replacement, StringComparison.Ordinal)))],
            compilation.Documents.IdentityCatalog);
}
