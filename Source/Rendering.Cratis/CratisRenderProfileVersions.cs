// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Resolves the scaffold profile version an artifact render profile is admitted against.
/// </summary>
static class CratisRenderProfileVersions
{
    /// <summary>
    /// Matches a profile against one explicit scaffold profile version.
    /// </summary>
    /// <param name="profileVersion">The scaffold profile version to match against, or null for the default.</param>
    /// <param name="admittedOptions">The options admitted when the profile matches.</param>
    /// <param name="mismatch">The reason the profile did not match.</param>
    /// <returns>True when the profile matches; otherwise false.</returns>
    internal delegate bool VersionMatcher(string? profileVersion, out CratisRenderingOptions? admittedOptions, out string mismatch);

    /// <summary>
    /// Matches the default version first and then the current version. Downstream of the facade the version is already admitted, so a profile exactly equal to any known profile is accepted.
    /// </summary>
    /// <param name="matcher">The matcher for a single version.</param>
    /// <param name="admittedOptions">The options admitted when the profile matches.</param>
    /// <param name="mismatch">The reason the profile did not match the default version.</param>
    /// <returns>True when the profile matches any known version; otherwise false.</returns>
    internal static bool MatchesAnyKnownVersion(VersionMatcher matcher, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
    {
        if (matcher(null, out admittedOptions, out mismatch) && admittedOptions is not null)
        {
            return true;
        }

        return matcher(CratisBackendApplicationScaffoldProfile.Current.Version, out admittedOptions, out _) && admittedOptions is not null;
    }

    /// <summary>
    /// Resolves the scaffold profile for a version and checks it against the version recorded by the supplied profile.
    /// </summary>
    /// <param name="profile">The supplied profile.</param>
    /// <param name="profileVersion">The requested scaffold profile version, or null for the default.</param>
    /// <param name="scaffoldProfile">The resolved scaffold profile.</param>
    /// <param name="mismatch">The reason the version could not be resolved.</param>
    /// <returns>True when the version resolves and does not conflict; otherwise false.</returns>
    internal static bool TryResolve(ArtifactRenderProfile profile, string? profileVersion, [NotNullWhen(true)] out CratisBackendApplicationScaffoldProfile? scaffoldProfile, out string mismatch)
    {
        mismatch = string.Empty;
        scaffoldProfile = CratisBackendApplicationScaffoldProfile.ForVersion(profileVersion);
        if (scaffoldProfile is null)
        {
            mismatch = $"The scaffold profile version '{profileVersion}' is not supported.";
            return false;
        }

        var recordedVersion = profile.Inputs.First(input => input.Name.StartsWith(CratisArtifactRenderProfileAdmission.ProjectInputPrefix, StringComparison.Ordinal) && input.Name.EndsWith(".csproj", StringComparison.Ordinal)).Version;
        if (profileVersion is not null && !string.Equals(recordedVersion, scaffoldProfile.Version, StringComparison.Ordinal))
        {
            mismatch = $"The scaffold profile version '{profileVersion}' conflicts with the version '{recordedVersion}' recorded by the supplied profile.";
            scaffoldProfile = null;
            return false;
        }

        return true;
    }
}
