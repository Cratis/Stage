// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Computes the lowest common ancestor of a set of module/feature paths — the folder level at which a value
/// shared by all of them first becomes visible to every one of them.
/// </summary>
public static class Placement
{
    /// <summary>
    /// Computes the lowest common ancestor path segments.
    /// </summary>
    /// <param name="paths">The paths to find the common ancestor of.</param>
    /// <returns>
    /// The longest shared path prefix — empty when the paths share no ancestor (i.e. they diverge at the very
    /// first segment, meaning the value is shared across top-level modules).
    /// </returns>
    public static IReadOnlyList<string> LowestCommonAncestor(IEnumerable<IReadOnlyList<string>> paths)
    {
        var pathList = paths.ToArray();
        if (pathList.Length == 0)
        {
            return [];
        }

        var common = pathList[0];
        foreach (var path in pathList.Skip(1))
        {
            var matched = 0;
            while (matched < common.Count && matched < path.Count && string.Equals(common[matched], path[matched], StringComparison.Ordinal))
            {
                matched++;
            }

            common = [.. common.Take(matched)];
        }

        return common;
    }
}
