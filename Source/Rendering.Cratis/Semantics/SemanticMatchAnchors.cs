// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Makes authored match anchors portable to Screenplay's ECMAScript end-of-input semantics.
/// </summary>
internal static class SemanticMatchAnchors
{
    /// <summary>
    /// Rewrites unescaped dollar anchors outside character classes as strict end-of-input assertions.
    /// </summary>
    /// <param name="pattern">The validated authored pattern.</param>
    /// <returns>The portable pattern.</returns>
    internal static string Portable(string pattern)
    {
        var result = new StringBuilder(pattern.Length);
        var classStart = -1;
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '\\')
            {
                result.Append(character).Append(pattern[++index]);
                continue;
            }

            if (character == '[' && classStart < 0)
            {
                // A leading ']' is literal; after '^' it closes '[^]', the any-character class.
                classStart = index + 1;
            }
            else if (character == ']' && index > classStart)
            {
                classStart = -1;
            }

            result.Append(character == '$' && classStart < 0 ? @"(?![\s\S])" : character.ToString());
        }

        return result.ToString();
    }
}
