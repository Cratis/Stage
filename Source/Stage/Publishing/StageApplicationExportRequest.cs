// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Publishing;

/// <summary>
/// Asks for an application package to be produced from in-memory Screenplay sources.
/// </summary>
/// <param name="ModelFiles">The <c language="shell">.play</c> sources, keyed by their relative path (forward slashes) with their text as the value.</param>
/// <param name="ApplicationName">The name of the application. It is explicit so that the package never depends on a folder name.</param>
/// <param name="ApplicationVersion">The semantic version of this package of the application.</param>
/// <param name="PublishedName">The host label the application is published under.</param>
public sealed record StageApplicationExportRequest(
    IReadOnlyDictionary<string, string> ModelFiles,
    string ApplicationName,
    string ApplicationVersion,
    string PublishedName)
{
    /// <summary>
    /// Gets the publication id, or <see langword="null"/> to derive one from the published name, version and model.
    /// </summary>
    public string? PublicationId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the package asks for the Notes backend.
    /// </summary>
    public bool NotesEnabled { get; init; }

    /// <summary>
    /// Gets the Stage runtime version the package requires, or <see langword="null"/> for this runtime's version.
    /// </summary>
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Gets the identifiers the source model gave to its elements, keyed by <c language="shell">kind:qualified.name</c>
    /// (kinds: module, feature, slice, screen). Empty when the source model supplied none.
    /// </summary>
    public IReadOnlyDictionary<string, string> ModelReferences { get; init; } = new Dictionary<string, string>();
}
