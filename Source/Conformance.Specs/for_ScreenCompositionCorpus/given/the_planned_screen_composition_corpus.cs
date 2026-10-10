// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus.given;

/// <summary>
/// The canonical screen composition corpus planned by Stage exactly as a caller that authored screens plans it:
/// one compilation, its executable semantic model for the backend and its translated Scene carried in the profile.
/// </summary>
public class the_planned_screen_composition_corpus : Specification
{
    protected const string ApplicationName = "Workspaces";

    /// <summary>
    /// Plans one source form of the corpus.
    /// </summary>
    /// <param name="documents">The documents of the source form.</param>
    /// <param name="catalogBytes">The source form's identity catalog, or empty to derive identities from the paths.</param>
    /// <returns>The plan and the Scene it carried.</returns>
    /// <param name="carryScene">Whether the translated Scene is carried in the profile, as a caller that authored screens does.</param>
    protected static (ArtifactRenderPlan Plan, SceneApplication Scene) Plan(IEnumerable<CanonicalCorpusDocument> documents, ReadOnlySpan<byte> catalogBytes, bool carryScene)
    {
        var sources = documents.ToArray();
        var catalog = catalogBytes.IsEmpty ? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName)) : SemanticIdentityCatalogSerializer.Deserialize(catalogBytes);
        var compilation = new SemanticModelCompiler().Compile(
            ApplicationName,
            SemanticDocumentSet.Create([.. sources.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text))], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));

        var scene = SceneFor(sources);
        var options = new CratisRenderingOptions(ApplicationName, ApplicationName);
        var request = new ArtifactRenderRequest(
            model,
            execution.Plan!,
            CratisRendering.CreateProfile(model.Application.Name, options, carryScene ? scene : null),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));

        return (new CratisArtifactRenderPlanner().Plan(request), scene);
    }

    /// <summary>
    /// Translates a source form to its Scene the way Stage loads authored screens.
    /// </summary>
    /// <param name="documents">The documents.</param>
    /// <returns>The translated Scene.</returns>
    protected static SceneApplication SceneOf(IEnumerable<CanonicalCorpusDocument> documents) => SceneFor([.. documents]);

    static SceneApplication SceneFor(IReadOnlyList<CanonicalCorpusDocument> documents)
    {
        var root = Path.Combine(Path.GetTempPath(), $"stage-screen-conformance-{Guid.NewGuid():N}");
        try
        {
            foreach (var document in documents)
            {
                var path = Path.Combine(root, document.DisplayPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. document.Bytes]);
            }

            var compilation = new PlayFileCompiler().CompileFolder(root).Result;
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

            return new ScreenplaySceneVisitor().Visit(compilation.Value!);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
