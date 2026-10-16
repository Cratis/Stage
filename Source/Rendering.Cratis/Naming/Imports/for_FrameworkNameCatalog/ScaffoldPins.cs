// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Lists the exact versions a scaffold profile pins, keyed the way the catalog records them.
/// </summary>
static class ScaffoldPins
{
    /// <summary>
    /// Gets the pins of a profile.
    /// </summary>
    /// <param name="profile">The scaffold profile.</param>
    /// <returns>The pins, keyed by package id or <c language="csharp">TargetFramework</c>.</returns>
    public static ImmutableSortedDictionary<string, string> Of(CratisBackendApplicationScaffoldProfile profile) =>
        new Dictionary<string, string>
        {
            ["TargetFramework"] = profile.TargetFramework,
            ["TargetingPackVersion"] = CatalogScaffold.ReferencePackVersion,
            ["Cratis"] = profile.CratisPackageVersion,
            ["Cratis.Arc.Chronicle.Testing"] = profile.CratisArcChronicleTestingPackageVersion,
            ["Cratis.Arc.MongoDB"] = profile.CratisArcMongoDBPackageVersion,
            ["Cratis.Chronicle"] = profile.CratisChroniclePackageVersion,
            ["Cratis.Chronicle.AspNetCore"] = profile.CratisChroniclePackageVersion,
            ["Cratis.Chronicle.Testing"] = profile.CratisChroniclePackageVersion,
            ["Cratis.Specifications"] = profile.CratisSpecificationsPackageVersion,
            ["Cratis.Specifications.XUnit"] = profile.CratisSpecificationsXUnitPackageVersion,
            ["Microsoft.NET.Test.Sdk"] = profile.MicrosoftNetTestSdkPackageVersion,
            ["NSubstitute"] = profile.NSubstitutePackageVersion,
            ["xunit"] = profile.XunitPackageVersion,
            ["xunit.runner.visualstudio"] = profile.XunitRunnerVisualStudioPackageVersion
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
}
