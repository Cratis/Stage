// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Assesses admitted release revisions through Screenplay's resolved-schema equality check, without writing sidecars.
/// </summary>
public static class StageReleaseBaseline
{
    /// <summary>
    /// Checks the whole authoring workspace, including unsaved source and historical event revisions.
    /// </summary>
    /// <param name="workspace">The candidate with authoritative identity provenance.</param>
    /// <param name="baseline">The read-only baseline outcome.</param>
    /// <returns>The nonblocking assessment for the workspace's semantic revision.</returns>
    public static ReleaseBaselineAssessment Check(ScreenplayWorkspace workspace, ReleaseBaselineRead baseline)
    {
        var revision = workspace.Compilation.Value?.Model.Revision ?? default;
        if (ReadOutcome(baseline, workspace.IdentityCatalog.Application, revision) is { } outcome) return outcome;
        if (!workspace.Compilation.Success) return Unavailable(revision, "Release baseline checks require an executable model.");
        if (!HasAuthoritativeIdentities(workspace.Compilation.Value!)) return Unavailable(revision, "Release baseline checks require authoritative document and event contract assignments.");

        var result = ReleaseBaseline.Check(workspace, baseline);
        if (result.Failure is not null) return new(ReleaseBaselineAssessmentStatus.Unavailable, revision, [], result.Reason ?? result.Failure);
        var entries = baseline.Document!.Entries.ToDictionary(entry => (entry.Event, entry.Revision));
        var violations = result.Violations.Select(violation =>
        {
            var entry = entries[(violation.Event, violation.Revision)];
            return new StageReleaseBaselineViolation(violation.Kind, EventContractId.Parse(violation.Event), violation.Revision, entry.EventType, entry.LastAddress, entry.AdmittedIn, violation.Path);
        });

        return new(ReleaseBaselineAssessmentStatus.Checked, revision, [.. violations]);
    }

    /// <summary>
    /// Reconstructs the exact source workspace from a compilation, requiring semantic-revision parity.
    /// </summary>
    /// <param name="compilation">The model and its original documents, identity catalog, and attachment contents.</param>
    /// <param name="baseline">The read-only baseline outcome.</param>
    /// <returns>The assessment, or unavailable when authoritative provenance or parity cannot be established.</returns>
    public static ReleaseBaselineAssessment Check(SemanticCompilation compilation, ReleaseBaselineRead baseline)
    {
        var revision = compilation.Model.Revision;
        var documents = compilation.Documents;
        if (ReadOutcome(baseline, documents.IdentityCatalog.Application, revision) is { } outcome) return outcome;
        if (!HasAuthoritativeIdentities(compilation)) return Unavailable(revision, "Release baseline checks require authoritative document and event contract assignments.");
        try
        {
            var workspace = ScreenplayWorkspace.Create(
                documents.IdentityCatalog.Application,
                compilation.Model.Application.Name,
                [.. documents.Documents.Select(document => WorkspaceDocument.Create(document.Id, document.StableKey, PortablePlayPath.Parse(document.DisplayPath), Encoding.UTF8.GetBytes(document.Text)))],
                documents.IdentityCatalog,
                documents.AttachmentContents,
                []);
            if (workspace.Compilation.Value?.Model.Revision != revision) return Unavailable(revision, "The reconstructed workspace does not match the compilation's semantic revision.");

            return Check(workspace, baseline);
        }
        catch (Exception exception) when (exception is InvalidScreenplayWorkspace or InvalidWorkspaceDocument or InvalidPortablePlayPath)
        {
            return Unavailable(revision, exception.Message);
        }
    }

    static ReleaseBaselineAssessment? ReadOutcome(ReleaseBaselineRead baseline, ApplicationIdentity application, SemanticRevision revision) => baseline switch
    {
        { Status: "absent" } => new(ReleaseBaselineAssessmentStatus.Absent, revision, []),
        { Status: "unreadable" } => new(ReleaseBaselineAssessmentStatus.Unreadable, revision, [], baseline.Reason),
        { Status: "valid", Document: { } document } when document.ApplicationIdentity == application.ToString() => null,
        _ => new(ReleaseBaselineAssessmentStatus.Unreadable, revision, [], baseline.Reason ?? "Unsupported release baseline or another application's identity.")
    };

    static bool HasAuthoritativeIdentities(SemanticCompilation compilation)
    {
        var catalog = compilation.Documents.IdentityCatalog;

        // An explicitly supplied catalog may retain LegacyBootstrap origins after persistence.
        // Require its actual assignments before reconstruction, never Resolve* fallbacks.
        return compilation.Documents.Documents.All(document => catalog.Documents.Any(assignment =>
                assignment.Id == document.Id && assignment.Key == document.StableKey)) &&
            compilation.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Events)
                .All(contract => catalog.EventContracts.Any(assignment => assignment.Id == contract.ContractId && assignment.Revision == contract.Revision));
    }

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static ReleaseBaselineAssessment Unavailable(SemanticRevision revision, string reason) => new(ReleaseBaselineAssessmentStatus.Unavailable, revision, [], reason);
}
