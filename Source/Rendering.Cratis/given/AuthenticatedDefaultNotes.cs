// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Drops the informational notes Stage reports when it infers an authenticated caller for an artifact without an
/// <c language="csharp">authorize</c> clause, so a spec about something other than authorization can still assert that rendering
/// reports nothing unexpected.
/// </summary>
/// <remarks>
/// Only the two inference codes are dropped. Warnings, errors and every other information diagnostic stay visible,
/// and specs about authorization itself must not use these helpers.
/// </remarks>
public static class AuthenticatedDefaultNotes
{
    const string AuthenticatedByDefault = "STAGE-ESM-054";
    const string SystemIdentityByDefault = "STAGE-ESM-055";

    // The legacy renderers report plain text without a code; these two phrases are the stable parts of their inference notes.
    const string LegacyNoteSubject = "requires an authenticated caller";
    const string LegacyNoteReason = "the model has no authorize clause";

    /// <summary>
    /// Gets the diagnostics without the notes about an inferred authenticated caller or system identity.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to filter.</param>
    /// <returns>The remaining diagnostics.</returns>
    public static IEnumerable<ArtifactRenderDiagnostic> WithoutAuthenticatedDefaultNotes(this IEnumerable<ArtifactRenderDiagnostic> diagnostics) =>
        diagnostics.Where(diagnostic => !IsNote(diagnostic.Code));

    /// <summary>
    /// Gets the plan diagnostics without the notes about an inferred authenticated caller or system identity.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to filter.</param>
    /// <returns>The remaining diagnostics.</returns>
    public static IEnumerable<CratisPlanDiagnostic> WithoutAuthenticatedDefaultNotes(this IEnumerable<CratisPlanDiagnostic> diagnostics) =>
        diagnostics.Where(diagnostic => !IsNote(diagnostic.Code));

    /// <summary>
    /// Gets the legacy renderer problem report without the lines noting an inferred authenticated caller.
    /// </summary>
    /// <param name="report">The text the renderer reported to its error writer.</param>
    /// <returns>The remaining report.</returns>
    public static string WithoutAuthenticatedDefaultNotes(this string report) =>
        string.Join(Environment.NewLine, report.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(line => !IsLegacyNote(line)));

    /// <summary>
    /// Gets the rendered file diagnostics without the notes about an inferred authenticated caller.
    /// </summary>
    /// <param name="diagnostics">The legacy renderer diagnostics.</param>
    /// <returns>The remaining diagnostics.</returns>
    public static IEnumerable<string> WithoutAuthenticatedDefaultNotes(this IEnumerable<string> diagnostics) =>
        diagnostics.Where(diagnostic => !IsLegacyNote(diagnostic));

    static bool IsLegacyNote(string text) =>
        text.Contains(LegacyNoteSubject, StringComparison.Ordinal) && text.Contains(LegacyNoteReason, StringComparison.Ordinal);

    static bool IsNote(string code) => string.Equals(code, AuthenticatedByDefault, StringComparison.Ordinal) || string.Equals(code, SystemIdentityByDefault, StringComparison.Ordinal);
}
