// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Semantics;

public sealed partial record ModelAssets
{
    /// <summary>
    /// Recognizes root file names Vite emits for the bundle: <c>index-&lt;hash&gt;.js|css</c> and <c>&lt;name&gt;-&lt;hash&gt;[.&lt;ext&gt;].map</c>.
    /// Plain string scanning has no backtracking and no timeout, so a hostile asset name can neither stall nor fail the load.
    /// </summary>
    static class BundleNames
    {
        const string EntryPrefix = "index-";
        const string SourceMapSuffix = ".map";

        internal static bool Matches(string name) =>
            !name.Contains('/') && (IsEntryChunk(name) || IsSourceMap(name));

        static bool IsEntryChunk(string name)
        {
            if (!name.StartsWith(EntryPrefix, StringComparison.Ordinal)) return false;
            var extension = name.LastIndexOf('.');
            if (extension <= EntryPrefix.Length) return false;
            var suffix = name[(extension + 1)..];
            return (string.Equals(suffix, "js", StringComparison.Ordinal) || string.Equals(suffix, "css", StringComparison.Ordinal)) && IsHash(name.AsSpan(EntryPrefix.Length, extension - EntryPrefix.Length));
        }

        static bool IsSourceMap(string name)
        {
            if (!name.EndsWith(SourceMapSuffix, StringComparison.Ordinal)) return false;
            var stem = name.AsSpan(0, name.Length - SourceMapSuffix.Length);
            for (var dash = stem.IndexOf('-'); dash >= 0; dash = NextDash(stem, dash))
            {
                if (dash > 0 && HasHashAfter(stem, dash + 1)) return true;
            }
            return false;
        }

        static int NextDash(ReadOnlySpan<char> stem, int dash)
        {
            var next = stem[(dash + 1)..].IndexOf('-');
            return next < 0 ? -1 : dash + 1 + next;
        }

        static bool HasHashAfter(ReadOnlySpan<char> stem, int start)
        {
            var end = start;
            while (end < stem.Length && IsHashCharacter(stem[end])) end++;
            if (end == start) return false;
            return end == stem.Length || (stem[end] == '.' && end + 1 < stem.Length);
        }

        static bool IsHash(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty) return false;
            foreach (var character in value)
            {
                if (!IsHashCharacter(character)) return false;
            }
            return true;
        }

        static bool IsHashCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '_' or '-';
    }
}
