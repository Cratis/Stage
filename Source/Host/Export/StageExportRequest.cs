// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Export;

/// <summary>
/// Asks for an application package to be produced from a model on disk.
/// </summary>
/// <param name="ModelPath">A <c language="shell">.play</c> file or a folder of them.</param>
/// <param name="ApplicationVersion">The semantic version of this package of the application.</param>
/// <param name="PublishedName">The host label the application is published under.</param>
/// <param name="OutputPath">The folder to write the package to. It must be empty or absent.</param>
public sealed record StageExportRequest(string ModelPath, string ApplicationVersion, string PublishedName, string OutputPath)
{
    /// <summary>
    /// Gets the application name, or <see langword="null"/> to take it from the model folder or file name.
    /// </summary>
    public string? ApplicationName { get; init; }

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
