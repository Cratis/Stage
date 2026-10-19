// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

// Published decision-0091 specifications, compiled in each canonical source form.
internal static class refused_read_keys
{
    internal static readonly string[] Names = ["RefusedEmptyKey", "RefusedWhitespaceKey", "RefusedStarKey", "RefusedHashKey", "RefusedPaddedKey", "RefusedStarKeyWithStoredInstance"];

    internal static SemanticExecutionPlan Compile(string formName = "single")
    {
        var corpus = ProtectedCommandReadsCorpus.V25;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Assert.Equal(6, plan.Specifications.Values.Count(specification => Names.Contains(specification.Name, StringComparer.Ordinal)));

        return plan;
    }
}
