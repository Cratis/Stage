// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Says whether the Chronicle client a rendered application is pinned to carries the graph projection API
/// (<c language="csharp">[Node]</c>, <c language="csharp">[Edge]</c>, <c language="csharp">GraphScenario</c>).
/// </summary>
/// <remarks>
/// The graph API is in Cratis/Chronicle#5680 and is not released yet, so <see cref="FirstRelease"/> is not set and the renderer refuses
/// every model that declares a graph with <see cref="NeedsChronicleGraphs"/>. When Chronicle ships it, set <see cref="FirstRelease"/> to
/// that version and bump the Chronicle pins in Directory.Packages.props; nothing else changes.
/// </remarks>
internal static class ChronicleGraphSupport
{
    /// <summary>
    /// The code for a graph declaration or graph projection that needs a Chronicle client Stage does not pin yet.
    /// </summary>
    internal const string NeedsChronicleGraphs = "STAGE-ESM-063";

    static readonly AsyncLocal<bool> _assumed = new();

    /// <summary>
    /// Gets the first Chronicle release that ships graph projections, or null while it is unreleased.
    /// </summary>
    internal static Version? FirstRelease { get; }

    /// <summary>
    /// Gets whether the Chronicle version Stage pins for rendered applications carries the graph API.
    /// </summary>
    internal static bool PinnedClientAdmits => _assumed.Value || Admits(Scaffolding.ScaffoldPackageVersions.Chronicle, FirstRelease);

    /// <summary>
    /// Gets whether a Chronicle client version carries the graph API.
    /// </summary>
    /// <param name="chronicleVersion">The Chronicle client package version.</param>
    /// <param name="firstRelease">The first release with graph projections, or null while unreleased.</param>
    /// <returns>True when the version is at or past the first release.</returns>
    internal static bool Admits(string chronicleVersion, Version? firstRelease) =>
        firstRelease is not null && Version.TryParse(chronicleVersion.Split('-', '+')[0], out var version) && version >= firstRelease;

    /// <summary>
    /// Plans, for the current asynchronous flow, as if the pinned Chronicle client carried the graph API. Specifications use it to read what
    /// the renderer produces for Chronicle's graph API before a Chronicle that ships it is pinned.
    /// </summary>
    /// <returns>The scope that restores the previous assumption when disposed.</returns>
    internal static IDisposable AssumeReleased()
    {
        var previous = _assumed.Value;
        _assumed.Value = true;
        return new Scope(previous);
    }

    sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => _assumed.Value = previous;
    }
}
