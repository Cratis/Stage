// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The packages every Stage frontend ships, which an approved custom package may not reuse the name of.
/// </summary>
/// <remarks>
/// The renderer's catalog and a live Stage's package host both read the names from here, so the packages a
/// generated application renders and the ones a running Stage renders are the same five. The PrimeReact widgets and the default
/// blueprint are shipped because the frontend is built from them: a domain blueprint package depends on the default blueprint,
/// and that dependency is satisfied by the Stage rather than by an approval.
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
    /// The PrimeReact widgets package the default blueprint is built from.
    /// </summary>
    public const string PrimeReact = "PrimeReact";

    /// <summary>
    /// The default blueprint: the application shell, the shell components and the foundation templates every domain blueprint builds on.
    /// </summary>
    public const string DefaultBlueprint = "Cratis.Blueprint.Default";

    /// <summary>
    /// Gets the names of the shipped packages.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = [Core, SceneWeb, CratisComponents, PrimeReact, DefaultBlueprint];
}
