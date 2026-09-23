// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SizeClasses;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Parses the bare size class names Screenplay carries as plain strings (<c language="csharp">target size compact</c>,
/// <c language="csharp">when width regular</c>, …) into their <see cref="WidthSizeClass"/>/<see cref="HeightSizeClass"/>
/// equivalents - part of Cratis/Stage#37.
/// </summary>
public static class SizeClassNames
{
    /// <summary>
    /// Parses a size class name as a <see cref="WidthSizeClass"/>.
    /// </summary>
    /// <param name="name">The size class name to parse.</param>
    /// <returns>The parsed <see cref="WidthSizeClass"/>.</returns>
    /// <exception cref="UnknownSizeClassName">Thrown when <paramref name="name"/> does not match a known member.</exception>
    public static WidthSizeClass ParseWidth(string name) =>
        Enum.TryParse<WidthSizeClass>(name, ignoreCase: true, out var value) ? value : throw new UnknownSizeClassName(name);

    /// <summary>
    /// Parses a size class name as a <see cref="HeightSizeClass"/>.
    /// </summary>
    /// <param name="name">The size class name to parse.</param>
    /// <returns>The parsed <see cref="HeightSizeClass"/>.</returns>
    /// <exception cref="UnknownSizeClassName">Thrown when <paramref name="name"/> does not match a known member.</exception>
    public static HeightSizeClass ParseHeight(string name) =>
        Enum.TryParse<HeightSizeClass>(name, ignoreCase: true, out var value) ? value : throw new UnknownSizeClassName(name);

    /// <summary>
    /// Parses the size a deployment target assumes by default.
    /// </summary>
    /// <param name="name">The name as authored.</param>
    /// <returns>The <see cref="TargetSizeClass"/>.</returns>
    /// <exception cref="UnknownSizeClassName">Thrown when the name is not one a target can assume.</exception>
    /// <remarks>
    /// A separate vocabulary from the two arrangement axes, and three-valued. Parsing a target's size with
    /// <see cref="ParseWidth"/> is what made <c language="csharp">target size expanded</c> - valid Screenplay, used in
    /// Screenplay's own documentation - throw instead of translating.
    /// </remarks>
    public static TargetSizeClass ParseTarget(string name) =>
        Enum.TryParse<TargetSizeClass>(name, ignoreCase: true, out var value) ? value : throw new UnknownSizeClassName(name);
}
