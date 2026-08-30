// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Represents the exact version profile used to scaffold a first-run Cratis backend application.
/// </summary>
public sealed class CratisBackendApplicationScaffoldProfile
{
    const string CurrentVersion = "1";

    CratisBackendApplicationScaffoldProfile(
        string version,
        string targetFramework,
        string cratisPackageVersion,
        string cratisArcMongoDBPackageVersion,
        string cratisArcChronicleTestingPackageVersion,
        string cratisSpecificationsPackageVersion,
        string cratisSpecificationsXUnitPackageVersion,
        string microsoftNetTestSdkPackageVersion,
        string nSubstitutePackageVersion,
        string xunitPackageVersion,
        string xunitRunnerVisualStudioPackageVersion,
        string chronicleImageVersion)
    {
        Version = version;
        TargetFramework = targetFramework;
        CratisPackageVersion = cratisPackageVersion;
        CratisArcMongoDBPackageVersion = cratisArcMongoDBPackageVersion;
        CratisArcChronicleTestingPackageVersion = cratisArcChronicleTestingPackageVersion;
        CratisSpecificationsPackageVersion = cratisSpecificationsPackageVersion;
        CratisSpecificationsXUnitPackageVersion = cratisSpecificationsXUnitPackageVersion;
        MicrosoftNetTestSdkPackageVersion = microsoftNetTestSdkPackageVersion;
        NSubstitutePackageVersion = nSubstitutePackageVersion;
        XunitPackageVersion = xunitPackageVersion;
        XunitRunnerVisualStudioPackageVersion = xunitRunnerVisualStudioPackageVersion;
        ChronicleImageVersion = chronicleImageVersion;
    }

    /// <summary>
    /// Gets the profile Stage currently supports and verifies.
    /// </summary>
    public static CratisBackendApplicationScaffoldProfile Current { get; } = Create(
        CurrentVersion,
        "net10.0",
        "22.3.0",
        "22.3.0",
        "22.3.0",
        "4.0.0",
        "4.0.0",
        "18.9.0",
        "6.2.0",
        "2.9.3",
        "4.0.0",
        "16.35.3");

    /// <summary>
    /// Gets the scaffold contract version carried by every generated input.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the exact target framework moniker.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Gets the exact <c>Cratis</c> package version.
    /// </summary>
    public string CratisPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>Cratis.Arc.MongoDB</c> package version.
    /// </summary>
    public string CratisArcMongoDBPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>Cratis.Arc.Chronicle.Testing</c> package version.
    /// </summary>
    public string CratisArcChronicleTestingPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>Cratis.Specifications</c> package version.
    /// </summary>
    public string CratisSpecificationsPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>Cratis.Specifications.XUnit</c> package version.
    /// </summary>
    public string CratisSpecificationsXUnitPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>Microsoft.NET.Test.Sdk</c> package version.
    /// </summary>
    public string MicrosoftNetTestSdkPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>NSubstitute</c> package version.
    /// </summary>
    public string NSubstitutePackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>xunit</c> package version.
    /// </summary>
    public string XunitPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>xunit.runner.visualstudio</c> package version.
    /// </summary>
    public string XunitRunnerVisualStudioPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c>cratis/chronicle</c> image version.
    /// </summary>
    public string ChronicleImageVersion { get; }

    /// <summary>
    /// Creates a validated immutable scaffold profile for in-assembly contract verification.
    /// </summary>
    /// <param name="version">The scaffold contract version.</param>
    /// <param name="targetFramework">The exact target framework moniker.</param>
    /// <param name="cratisPackageVersion">The exact <c>Cratis</c> package version.</param>
    /// <param name="cratisArcMongoDBPackageVersion">The exact <c>Cratis.Arc.MongoDB</c> package version.</param>
    /// <param name="cratisArcChronicleTestingPackageVersion">The exact <c>Cratis.Arc.Chronicle.Testing</c> package version.</param>
    /// <param name="cratisSpecificationsPackageVersion">The exact <c>Cratis.Specifications</c> package version.</param>
    /// <param name="cratisSpecificationsXUnitPackageVersion">The exact <c>Cratis.Specifications.XUnit</c> package version.</param>
    /// <param name="microsoftNetTestSdkPackageVersion">The exact <c>Microsoft.NET.Test.Sdk</c> package version.</param>
    /// <param name="nSubstitutePackageVersion">The exact <c>NSubstitute</c> package version.</param>
    /// <param name="xunitPackageVersion">The exact <c>xunit</c> package version.</param>
    /// <param name="xunitRunnerVisualStudioPackageVersion">The exact <c>xunit.runner.visualstudio</c> package version.</param>
    /// <param name="chronicleImageVersion">The exact <c>cratis/chronicle</c> image version.</param>
    /// <returns>The validated profile.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when a version or target framework is not exact and supported.</exception>
    internal static CratisBackendApplicationScaffoldProfile Create(
        string version,
        string targetFramework,
        string cratisPackageVersion,
        string cratisArcMongoDBPackageVersion,
        string cratisArcChronicleTestingPackageVersion,
        string cratisSpecificationsPackageVersion,
        string cratisSpecificationsXUnitPackageVersion,
        string microsoftNetTestSdkPackageVersion,
        string nSubstitutePackageVersion,
        string xunitPackageVersion,
        string xunitRunnerVisualStudioPackageVersion,
        string chronicleImageVersion)
    {
        if (!IsPositiveInteger(version))
        {
            throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires a positive integer contract version.");
        }

        if (!IsTargetFramework(targetFramework))
        {
            throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires an exact net<major>.<minor> target framework.");
        }

        var exactVersions = new[]
        {
            cratisPackageVersion,
            cratisArcMongoDBPackageVersion,
            cratisArcChronicleTestingPackageVersion,
            cratisSpecificationsPackageVersion,
            cratisSpecificationsXUnitPackageVersion,
            microsoftNetTestSdkPackageVersion,
            nSubstitutePackageVersion,
            xunitPackageVersion,
            xunitRunnerVisualStudioPackageVersion,
            chronicleImageVersion
        };
        if (exactVersions.Any(versionValue => !IsExactStableVersion(versionValue)))
        {
            throw new InvalidCratisBackendApplicationScaffold("A scaffold profile requires exact stable major.minor.patch package and image versions.");
        }

        return new(
            version,
            targetFramework,
            cratisPackageVersion,
            cratisArcMongoDBPackageVersion,
            cratisArcChronicleTestingPackageVersion,
            cratisSpecificationsPackageVersion,
            cratisSpecificationsXUnitPackageVersion,
            microsoftNetTestSdkPackageVersion,
            nSubstitutePackageVersion,
            xunitPackageVersion,
            xunitRunnerVisualStudioPackageVersion,
            chronicleImageVersion);
    }

    static bool IsTargetFramework(string value) =>
        value?.StartsWith("net", StringComparison.Ordinal) == true && IsExactVersion(value[3..], 2);

    static bool IsExactStableVersion(string value) => IsExactVersion(value, 3);

    static bool IsExactVersion(string value, int componentCount)
    {
        if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        var components = value.Split('.');
        return components.Length == componentCount && components.All(IsNonNegativeInteger);
    }

    static bool IsPositiveInteger(string value) =>
        IsNonNegativeInteger(value) && value.Any(character => character != '0');

    static bool IsNonNegativeInteger(string value) =>
        !string.IsNullOrEmpty(value) &&
        (value.Length == 1 || value[0] != '0') &&
        value.All(character => character is >= '0' and <= '9');
}
