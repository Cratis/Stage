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

    static SemanticModelLoadResult LoadModelRoot(string root, string[] files, IEnumerable<string> namedFiles, string name, CancellationToken cancellationToken)
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
        var documentPaths = workspace.Documents.Select(document => document.Path.Value).ToHashSet(StringComparer.Ordinal);

        // Screenplay's model root skips folders such as .git, .ai-work, bin, obj and node_modules. Files found by walking a folder
        // there are not part of the model, exactly as for `screenplay baseline check`; a file the caller named explicitly is refused.
        var named = namedFiles.Select(file => Path.GetRelativePath(root, file).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        selectedPaths.RemoveWhere(path => !documentPaths.Contains(path) && !named.Contains(path));
        if (selectedPaths.Count == 0) return Failure("STAGE-PLAN-002", "No .play files that the Screenplay model root reads were found.");
        var unread = selectedPaths.Where(path => !documentPaths.Contains(path)).Order(StringComparer.Ordinal).ToArray();
        if (unread.Length > 0)
        {
            return new(null, [.. unread.Select(path => new SemanticModelLoadDiagnostic("STAGE-PLAN-001", "The selected source is not part of the Screenplay model root (it is in a folder Screenplay does not read), so it cannot be planned with the root's persisted identities.", path))]);
        }

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
