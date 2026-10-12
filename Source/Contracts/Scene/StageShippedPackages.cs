// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The packages every Stage frontend ships, which an approved custom package may not reuse the name of.
/// </summary>
/// <remarks>
/// The renderer's catalog and a live Stage's package host both read the names from here, so the packages a
/// generated application renders and the ones a running Stage renders are the same three.
/// </remarks>
public static class StageShippedPackages
{
    /// <summary>
    /// The Scene core vocabulary package.
    /// </summary>
    public const string Core = "core";

    /// <summary>
    /// The Scene web components package.
    /// </summary>
    public const string SceneWeb = "scene.web";

    /// <summary>
    /// The Cratis component library package.
    /// </summary>
    public const string CratisComponents = "Cratis.Components";

    /// <summary>
    /// Gets the names of the shipped packages.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = [Core, SceneWeb, CratisComponents];
}
