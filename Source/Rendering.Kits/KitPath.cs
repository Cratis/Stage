// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Validates portable, ordinal kit-relative paths without rewriting them.</summary>
public static class KitPath
{
    /// <summary>Gets a rejection code, or null for a valid path.</summary>
    /// <param name="path">Path to validate.</param>
    /// <returns>The rejection code, if any.</returns>
    public static string? Validate(string path)
    {
        if (IsRooted(path))
        {
            return KitCodes.RootedPath;
        }

        if (path.Split('/').Any(IsTraversalSegment))
        {
            return KitCodes.Traversal;
        }

        if (!IsPortable(path))
        {
            return KitCodes.NonPortablePath;
        }

        return null;
    }

    internal static bool FitsArchive(string path) => path.Length <= 100 || path.Select((character, index) => (character, index)).Any(part => part.character == '/' && part.index <= 155 && path.Length - part.index - 1 <= 100);

    static bool IsTraversalSegment(string segment) => segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal);

    static bool IsRooted(string path) => path.StartsWith('/') || path.StartsWith('\\') || (path.Length >= 2 && path[1] == ':');

    static bool IsPortable(string path) => path.Length <= 255 && path.All(IsPortableCharacter) && FitsArchive(path);

    static bool IsPortableCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '/' or '.' or '_' or '-';
}
