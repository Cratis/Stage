// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 service layout input: the application is one service of a multi-service repository and every
/// artifact is placed under <c language="csharp">Source/{ProjectName}/</c>. Its absence, the default, keeps the
/// single-application layout at the application root.
/// </summary>
public static class ServiceLayoutInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:service-layout";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    static readonly byte[] _payload = new UTF8Encoding(false).GetBytes("service");

    /// <summary>
    /// Creates the service layout input.
    /// </summary>
    /// <returns>The hashed profile input.</returns>
    public static ArtifactRenderInput Create() => ArtifactRenderInput.Create(Name, Version, [.. _payload]);

    /// <summary>
    /// Reads whether a profile selects the service layout.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="layout">True when the profile carries the service layout input, false when it carries none.</param>
    /// <returns>Whether the profile carries at most one canonical service layout input.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out bool layout)
    {
        layout = false;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;

        layout = inputs is [var only] && IsCanonical(only);
        return layout;
    }

    /// <summary>
    /// Reads the layout a profile selects for the options it carries.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="options">The options the profile carries.</param>
    /// <param name="layout">Whether the profile selects the service layout.</param>
    /// <returns>Whether the input is canonical and not combined with the repository root.</returns>
    internal static bool TryAdmit(ArtifactRenderProfile profile, CratisRenderingOptions options, out bool layout) =>
        TryRead(profile, out layout) && !(layout && options.RepositoryRoot);

    /// <summary>
    /// Adds the service layout input to a profile.
    /// </summary>
    /// <param name="profile">A profile that does not already carry the input.</param>
    /// <param name="options">The options the profile is created for.</param>
    /// <returns>The profile carrying the input when the options select the service layout, otherwise the profile.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">The service layout is combined with the repository root.</exception>
    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, CratisRenderingOptions options)
    {
        if (!options.ServiceLayout) return profile;
        if (options.RepositoryRoot) throw new InvalidCratisBackendApplicationScaffold("ServiceLayout and RepositoryRoot cannot be combined; the repository plan owns repository-root files.");

        return ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, Create()]);
    }

    static bool IsCanonical(ArtifactRenderInput input) =>
        input.Version == Version && input.Bytes.AsSpan().SequenceEqual(_payload);
}
