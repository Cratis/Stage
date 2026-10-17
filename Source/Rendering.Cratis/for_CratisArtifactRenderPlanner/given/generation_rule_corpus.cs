// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class generation_rule_corpus
{
    internal static LoadedSemanticModel Compile(string body, string formName = "single", bool ruleOnly = false)
    {
        var corpus = GenerationFixturesCorpus.V21;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath,
            document.Text.Replace("return value;", body, StringComparison.Ordinal))).ToArray();
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        var model = compiled.Value!.Model;
        Assert.Equal(corpus.SemanticRevision, model.Revision);
        Assert.Equal(corpus.EsmBytes.ToArray(), SemanticModelSerializer.Serialize(model));

        if (ruleOnly)
        {
            // Other corpus specifications deliberately contain invalid generation fixtures. Recompile a
            // selection after checking the frozen full model, so the compiler owns the typed sidecar.
            documents = [.. form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath,
                SelectSpecification(document.Text).Replace("return value;", body, StringComparison.Ordinal)))];
            compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
            model = compiled.Value!.Model;
        }

        return new(model, SemanticExecutionPlan.Compile(model).Plan!)
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(compiled.Value.Documents, compiled.ImplementationRequirements),
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
    }

    static string SelectSpecification(string source)
    {
        const string prefix = "      specification ";
        var declarations = source[..source.IndexOf(prefix, StringComparison.Ordinal)];
        var start = source.IndexOf(prefix + "GeneratorBodyNotExecuted", StringComparison.Ordinal);
        var end = source.IndexOf(prefix, start + prefix.Length, StringComparison.Ordinal);

        return declarations + source[start..end];
    }
}
#endif
