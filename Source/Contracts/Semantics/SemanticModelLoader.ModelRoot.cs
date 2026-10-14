// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics;

public static partial class SemanticModelLoader
{
    static SemanticIdentityCatalog DeserializeCatalog(byte[] bytes, out PersistedIdentityState? state)
    {
        state = null;
        if (!IsPersistedIdentityState(bytes)) return SemanticIdentityCatalogSerializer.Deserialize(bytes);
        state = PersistedIdentityStateSerializer.Deserialize(bytes);
        return state.IdentityCatalog;
    }

    static bool IsPersistedIdentityState(byte[] bytes)
    {
        try
        {
            using var json = JsonDocument.Parse(bytes);
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("schema", out var schema) && schema.ValueKind == JsonValueKind.String &&
                schema.ValueEquals(PersistedIdentityStateSerializer.Schema);
        }
        catch (JsonException)
        {
            // Let the original catalog reader diagnose malformed bytes, preserving its existing message.
            return false;
        }
    }

    static bool IsRootIdentityPath(string root, string path) => string.Equals(
        Path.GetFullPath(path, root),
        Path.Combine(root, ".screenplay", PersistedIdentityStateSerializer.FileName),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    static SemanticModelLoadResult LoadModelRoot(string root, string[] files, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(root);
        if (!outcome.Success)
        {
            var code = outcome.FailureKind == "IdentityStateConflict" ? "STAGE-BASELINE-003" : "STAGE-BASELINE-004";
            return Failure(code, outcome.Reason, ".screenplay/identities.json");
        }

        var workspace = outcome.Workspace;
        var selectedPaths = files.Select(file => Path.GetRelativePath(root, file).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        var documents = workspace.Documents.Where(document => selectedPaths.Contains(document.Path.Value)).ToImmutableArray();
        var wholeRoot = documents.Length == workspace.Documents.Length && name == workspace.ApplicationName;
        var compiled = wholeRoot ? workspace.Compilation : CompileSelection(workspace, documents, name);
        var assessment = StageReleaseBaseline.Check(workspace, outcome.ReleaseBaseline);
        if (!wholeRoot)
        {
            assessment = new(
                ReleaseBaselineAssessmentStatus.Unavailable,
                compiled.Value?.Model.Revision ?? default,
                [],
                "Release baseline checks require the whole model root with its persisted application name.");
        }

        return FinishCompilation(compiled, workspace.AttachmentDiagnostics, assessment, cancellationToken);
    }

    static CompilationResult<SemanticCompilation> CompileSelection(ScreenplayWorkspace workspace, ImmutableArray<WorkspaceDocument> documents, string name) =>
        new SemanticModelCompiler().Compile(name, SemanticDocumentSet.Create(
            [.. documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text))],
            workspace.IdentityCatalog,
            workspace.AttachmentContents));
}
