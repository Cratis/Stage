// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Places the artifacts of one application in <c language="csharp">Source/{ProjectName}/</c> when its profile selects the
/// service layout, and leaves out the files the repository plan owns.
/// </summary>
/// <remarks>
/// This is the only place the layout is applied, after namespaces, folder placement and collision checks are decided, so a
/// service folder holds today's application byte for byte. Without the layout input every method returns its input unchanged.
/// </remarks>
static class ServiceLayout
{
    /// <summary>
    /// The folder that holds every service of a repository.
    /// </summary>
    public const string Root = "Source";

    static readonly string[] _repositoryFiles = [".editorconfig", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "docker-compose.yml"];

    /// <summary>
    /// Gets the service folder a profile selects.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <returns>The folder with a trailing slash, or an empty string when the profile does not select the service layout.</returns>
    public static string Folder(ArtifactRenderProfile profile) =>
        ServiceLayoutInput.TryRead(profile, out var layout) && layout && CratisArtifactRenderProfileAdmission.TryGetOptions(profile, out var options)
            ? $"{Root}/{options!.ProjectName}/"
            : string.Empty;

    /// <summary>
    /// Applies the layout carried by a profile to the planned artifacts.
    /// </summary>
    /// <param name="profile">The admitted render profile.</param>
    /// <param name="artifacts">The planned artifacts.</param>
    /// <returns>The artifacts, moved into the service folder with the repository-owned files left out when the profile selects the layout.</returns>
    public static ImmutableArray<PlannedArtifact> Apply(ArtifactRenderProfile profile, IEnumerable<PlannedArtifact> artifacts)
    {
        var folder = Folder(profile);
        if (folder.Length == 0) return [.. artifacts];
        var solution = $"{folder[(Root.Length + 1)..^1]}.slnx";

        return [.. artifacts.Where(artifact => !IsRepositoryFile(artifact.RelativePath, solution)).Select(artifact => Move(folder, artifact))];
    }

    /// <summary>
    /// Places a plan's artifacts, implementation-gap stubs and required removals under the layout its request profile selects.
    /// </summary>
    /// <param name="request">The render request used to create the plan, preserving its identity, revision and scope metadata.</param>
    /// <param name="plan">The plan with application-relative paths.</param>
    /// <returns>The placed plan, preserving diagnostics and refusals, or the same plan when no service folder is selected.</returns>
    public static ArtifactRenderPlan Plan(ArtifactRenderRequest request, ArtifactRenderPlan plan)
    {
        var folder = Folder(request.Profile);
        if (folder.Length == 0) return plan;

        return ArtifactRenderPlan.Create(
            request,
            Apply(request.Profile, plan.Artifacts),
            plan.Diagnostics,
            [.. plan.ImplementationGaps.Select(gap => gap with { Stubs = [.. gap.Stubs.Select(stub => stub with { RelativePath = folder + stub.RelativePath })] })],
            plan.Refusals,
            [.. plan.RequiredRemovals.Select(removal => removal with { RelativePath = folder + removal.RelativePath })]);
    }

    /// <summary>
    /// Maps a path an unplaced renderer produced to its place under the layout a profile selects.
    /// </summary>
    /// <param name="profile">The admitted render profile.</param>
    /// <param name="relativePath">The application-relative path.</param>
    /// <returns>The path, prefixed with the service folder when the profile selects the layout.</returns>
    public static string Map(ArtifactRenderProfile profile, string relativePath) => Folder(profile) + relativePath;

    /// <summary>
    /// Gets the service folder every path of a planned set shares.
    /// </summary>
    /// <param name="paths">The planned relative paths.</param>
    /// <returns>The shared <c language="csharp">Source/{Service}/</c> folder with a trailing slash, or an empty string when the paths do not share one.</returns>
    public static string SharedFolder(IEnumerable<string> paths)
    {
        var folders = paths.Select(path => path.Split('/')).Select(parts => parts.Length > 2 && parts[0] == Root ? $"{Root}/{parts[1]}/" : string.Empty).Distinct(StringComparer.Ordinal).ToArray();
        return folders is [{ Length: > 0 } shared] ? shared : string.Empty;
    }

    static bool IsRepositoryFile(string relativePath, string solution) =>
        Array.IndexOf(_repositoryFiles, relativePath) >= 0 || relativePath == solution;

    static PlannedArtifact Move(string folder, PlannedArtifact artifact) => artifact.WithRelativePath(folder + artifact.RelativePath);
}
