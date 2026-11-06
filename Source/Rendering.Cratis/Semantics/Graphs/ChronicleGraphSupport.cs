// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Says whether the Chronicle client a rendered application is pinned to carries the graph projection API
/// (<c language="csharp">[Node]</c>, <c language="csharp">[Edge]</c>, <c language="csharp">GraphScenario</c>).
/// </summary>
/// <remarks>
/// The graph API ships from Chronicle 19.129.0 (Cratis/Chronicle#5680). The renderer refuses every model that declares a graph with
/// <see cref="NeedsChronicleGraphs"/> when the Chronicle client Stage pins is older than <see cref="FirstRelease"/>.
/// </remarks>
internal static class ChronicleGraphSupport
{
    /// <summary>
    /// The code for a graph declaration or graph projection that needs a Chronicle client older than the one that ships the graph API.
    /// </summary>
    internal const string NeedsChronicleGraphs = "STAGE-ESM-063";

    /// <summary>
    /// Gets the first Chronicle release that ships graph projections.
    /// </summary>
    internal static Version? FirstRelease { get; } = new(19, 129, 0);

    /// <summary>
    /// Gets whether the Chronicle version Stage pins for rendered applications carries the graph API.
    /// </summary>
    internal static bool PinnedClientAdmits => Admits(Scaffolding.ScaffoldPackageVersions.Chronicle, FirstRelease);

    /// <summary>
    /// Gets whether a Chronicle client version carries the graph API.
    /// </summary>
    /// <param name="chronicleVersion">The Chronicle client package version.</param>
    /// <param name="firstRelease">The first release with graph projections.</param>
    /// <returns>True when the version is at or past the first release.</returns>
    internal static bool Admits(string chronicleVersion, Version? firstRelease) =>
        firstRelease is not null && Version.TryParse(chronicleVersion.Split('-', '+')[0], out var version) && version >= firstRelease;
}
