// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

// Source-authored decision-0091 vectors from Screenplay v4.209.0, while the package/corpus pin remains 4.206.0.
internal static class refused_read_keys
{
    internal static readonly string[] Names = ["RefusedEmptyKey", "RefusedWhitespaceKey", "RefusedStarKey", "RefusedHashKey", "RefusedPaddedKey", "RefusedStarKeyWithStoredInstance"];

    const string Specifications = """
              specification RefusedEmptyKey
                when Consume
                  id = "target"
                  sourceId = ""
                then error "Current '' does not exist."
              specification RefusedWhitespaceKey
                when Consume
                  id = "target"
                  sourceId = "   "
                then error "Current '   ' does not exist."
              specification RefusedStarKey
                when Consume
                  id = "target"
                  sourceId = "*"
                then error "Current '*' does not exist."
              specification RefusedHashKey
                when Consume
                  id = "target"
                  sourceId = "a#b"
                then error "Current 'a#b' does not exist."
              specification RefusedPaddedKey
                when Consume
                  id = "target"
                  sourceId = " padded "
                then error "Current ' padded ' does not exist."
              specification RefusedStarKeyWithStoredInstance
                given Stored
                  for "*"
                  value = "present"
                when Consume
                  id = "target"
                  sourceId = "*"
                then error "Current '*' does not exist."
        """;

    internal static SemanticExecutionPlan Compile(string formName = "single")
    {
        var corpus = ProtectedCommandReadsCorpus.V25;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            document.Text.Replace("    slice StateView State", $"{Specifications}\n    slice StateView State", StringComparison.Ordinal)));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Assert.Equal(6, plan.Specifications.Values.Count(specification => Names.Contains(specification.Name, StringComparer.Ordinal)));

        return plan;
    }
}
