// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// Checks lexical and symbolic-link containment before any output directories or files are created.
/// </summary>
/// <remarks>
/// Comparisons are ordinal on every platform, deliberately refusing case-only aliases even on case-insensitive
/// file systems. The root and candidate are resolved component by component using raw link targets, so parent
/// segments apply to the resolved directory rather than the lexical link path, including aliases such as macOS
/// /var. Each resolution permits at most 40 link hops. The resolved candidate must be strictly below the resolved
/// root; dangling links are allowed only when their resolved targets remain inside it. These checks inspect
/// existing links, not concurrent file-system changes after validation.
/// </remarks>
internal static class OutputRootContainment
{
    const int MaxLinkHops = 40;

    /// <summary>
    /// Resolves a generated path and verifies that it stays strictly inside the output root.
    /// </summary>
    /// <param name="targetDirectory">The output root.</param>
    /// <param name="relativePath">The generated file's supplied path.</param>
    /// <returns>The normalized absolute file path.</returns>
    /// <exception cref="GeneratedPathOutsideOutputRoot">The path escapes the root or resolution exceeds 40 link hops.</exception>
    internal static string Resolve(DirectoryInfo targetDirectory, string relativePath)
    {
        var root = Path.GetFullPath(targetDirectory.FullName);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!IsWithinRoot(candidate, root))
        {
            throw new GeneratedPathOutsideOutputRoot(relativePath, root);
        }

        var resolvedRoot = ResolvePath(root, relativePath, root);
        var resolvedCandidate = ResolvePath(candidate, relativePath, root);
        if (!IsWithinRoot(resolvedCandidate, resolvedRoot))
        {
            throw new GeneratedPathOutsideOutputRoot(relativePath, root);
        }

        return candidate;
    }

    static bool IsWithinRoot(string path, string root)
    {
        path = Path.TrimEndingDirectorySeparator(path);
        root = Path.TrimEndingDirectorySeparator(root);
        if (string.Equals(path, root, StringComparison.Ordinal))
        {
            return false;
        }

        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.Ordinal);
    }

    static string ResolvePath(string path, string relativePath, string root)
    {
        var current = Path.GetPathRoot(path)!;
        var remaining = new LinkedList<string>(Components(path[current.Length..]));
        var linkHops = 0;
        while (remaining.First is { } first)
        {
            remaining.RemoveFirst();
            var next = Step(current, first.Value);
            var target = LinkTargetOf(next);
            if (target is null)
            {
                current = next;
                continue;
            }

            if (++linkHops > MaxLinkHops)
            {
                throw new GeneratedPathOutsideOutputRoot(relativePath, root);
            }

            current = FollowLink(current, target, remaining);
        }

        return current;
    }

    static string Step(string current, string component) => component switch
    {
        "." => current,
        ".." => Directory.GetParent(current)?.FullName ?? current,
        _ => Path.Combine(current, component)
    };

    static string? LinkTargetOf(string path) => new FileInfo(path).LinkTarget ?? new DirectoryInfo(path).LinkTarget;

    static string FollowLink(string current, string target, LinkedList<string> remaining)
    {
        var targetRoot = Path.GetPathRoot(target);
        if (!string.IsNullOrEmpty(targetRoot))
        {
            current = targetRoot;
            target = target[targetRoot.Length..];
        }

        foreach (var component in Components(target).Reverse())
        {
            remaining.AddFirst(component);
        }

        return current;
    }

    static string[] Components(string path) => path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
}
