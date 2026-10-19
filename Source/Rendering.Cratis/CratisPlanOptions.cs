// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines application-level names, an optional caller profile, and domain placement for a source plan.
/// </summary>
/// <param name="ApplicationName">The application identity used for compilation and persistent stores.</param>
/// <param name="ProjectName">The application project and solution file name.</param>
/// <param name="RootNamespace">The application C# root namespace.</param>
public sealed record CratisPlanOptions(string ApplicationName, string ProjectName, string RootNamespace)
{
    /// <summary>
    /// Gets the domain sub-path, such as Sales or Sales/Retail. Empty preserves existing placement.
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// Gets the requested gap policy. Refuse is the default. This option has no effect until the target supports gap rendering.
    /// </summary>
    public ImplementationGapMode ImplementationGaps { get; init; } = ImplementationGapMode.Refuse;

    /// <summary>
    /// Gets the refusal policy. RenderAround permits only supported specification omissions.
    /// </summary>
    public ArtifactRefusalMode Refusals { get; init; } = ArtifactRefusalMode.Refuse;

    /// <summary>
    /// Gets a value indicating whether the application root is the repository root. When true, the first-run scaffold
    /// also contains the repository's continuous integration files under <c>.github/</c>; the default adds none.
    /// The caller decides, because planning never inspects the file system.
    /// </summary>
    public bool RepositoryRoot { get; init; }

    /// <summary>
    /// Gets the caller's complete Cratis profile for source and scaffold planning, or null to use the default profile.
    /// The profile must match the application names and omit domain placement, which is owned by <see cref="Domain"/>.
    /// Scaffold-only planning includes its strings catalog but reports Scene output as application-scope only.
    /// </summary>
    public ArtifactRenderProfile? Profile { get; init; }
}
