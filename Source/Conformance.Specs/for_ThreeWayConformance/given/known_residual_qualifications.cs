// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// The rendered files that still spell a type with <c language="csharp">global::</c> although nothing about the file
/// explains it. Each entry names the files, the reason and the follow-up that removes it; the list can only shrink.
/// A qualification in a file whose scope is unknown, or of a name that collides, never needs an entry.
/// </summary>
static class known_residual_qualifications
{
    /// <summary>
    /// The known residuals.
    /// </summary>
    public static readonly IReadOnlyList<KnownResidual> All =
    [
    ];

    /// <summary>
    /// Determines whether a residual is explained: the file's scope is unknown, the simple name collides, or an entry covers it.
    /// </summary>
    /// <param name="residual">The residual.</param>
    /// <param name="scope">Why the file's scope is unknown.</param>
    /// <returns><see langword="true"/> when explained.</returns>
    public static bool IsExplained(ResidualQualification residual, UnknownScopeReason scope) =>
        scope != UnknownScopeReason.None || residual.Collides || All.Any(known => known.Covers(residual));
}

/// <summary>
/// A residual that is known and tracked.
/// </summary>
/// <param name="PathContains">A fragment of the rendered file path, or empty for any file.</param>
/// <param name="Name">The simple type name, or empty for any.</param>
/// <param name="Reason">Why the file keeps the qualification.</param>
/// <param name="FollowUp">The work that removes it.</param>
sealed record KnownResidual(string PathContains, string Name, string Reason, string FollowUp)
{
    /// <summary>
    /// Determines whether this entry covers a residual.
    /// </summary>
    /// <param name="residual">The residual.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public bool Covers(ResidualQualification residual) =>
        residual.Path.Contains(PathContains, StringComparison.Ordinal) && (Name.Length == 0 || residual.Name == Name);
}
#endif
