// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Computes the namespace, type name, file name and folder path for a slice, mirroring the vertical-slice
/// convention where namespace and folder structure both follow the module/feature path.
/// </summary>
public static class SliceNaming
{
    /// <summary>
    /// Computes the namespace for a slice.
    /// </summary>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <param name="path">The module/feature path segments leading to the slice.</param>
    /// <returns>The computed namespace.</returns>
    public static string Namespace(string rootNamespace, IEnumerable<string> path) =>
        string.Join('.', new[] { rootNamespace }.Concat(path.Select(Identifiers.ToPascalCase)));

    /// <summary>
    /// Computes the type name for a slice.
    /// </summary>
    /// <param name="sliceName">The name of the slice.</param>
    /// <returns>The computed type name.</returns>
    public static string TypeName(string sliceName) => Identifiers.ToPascalCase(sliceName);

    /// <summary>
    /// Computes the file name for a slice.
    /// </summary>
    /// <param name="sliceName">The name of the slice.</param>
    /// <returns>The computed file name.</returns>
    public static string FileName(string sliceName) => $"{TypeName(sliceName)}.cs";

    /// <summary>
    /// Computes the folder path segments for a slice.
    /// </summary>
    /// <param name="path">The module/feature path segments leading to the slice.</param>
    /// <returns>The computed folder path segments.</returns>
    public static IReadOnlyList<string> FolderPath(IEnumerable<string> path) => [.. path.Select(Identifiers.ToPascalCase)];
}
