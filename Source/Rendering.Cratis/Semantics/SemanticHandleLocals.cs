// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Allocates stable Handle locals without shadowing generated-value parameters.
/// </summary>
/// <param name="command">The command whose generated parameters are reserved.</param>
internal sealed class SemanticHandleLocals(SemanticCommand command)
{
    readonly HashSet<string> _reserved = new(
        command.Properties.Where(property => property.IsGenerated).Select(property => SemanticCommandResultRendering.PropertyValue(property).TrimStart('@')),
        StringComparer.Ordinal);
    readonly Dictionary<string, string> _allocated = new(StringComparer.Ordinal);

    internal string Name(string preferred) => Allocate(preferred, preferred);

    /// <summary>Allocates the local that holds one handler production, never aliasing a named local.</summary>
    /// <param name="position">Declared position of the production.</param>
    /// <param name="preferred">Preferred local name.</param>
    /// <returns>The allocated local name.</returns>
    internal string Production(int position, string preferred) => Allocate($"\u0001production:{position}", preferred);

    string Allocate(string key, string preferred)
    {
        if (_allocated.TryGetValue(key, out var allocated)) return allocated;
        var candidate = preferred;
        var suffix = 1;
        while (!_reserved.Add(candidate))
        {
            candidate = $"{preferred}{suffix++}";
        }
        _allocated.Add(key, candidate);

        return candidate;
    }
}
