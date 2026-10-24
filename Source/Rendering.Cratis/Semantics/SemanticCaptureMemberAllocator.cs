// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Allocates one capture's generated declarations without stealing another declaration's preferred name.
/// </summary>
/// <param name="preferred">All preferred member spellings to reserve before allocation.</param>
/// <param name="occupied">Declarations whose identifiers cannot change.</param>
internal sealed class SemanticCaptureMemberAllocator(IEnumerable<string> preferred, IEnumerable<string> occupied)
{
    readonly HashSet<string> _reserved = new(preferred.Select(SemanticCaptureIdentifier.Decode), StringComparer.Ordinal);
    readonly HashSet<string> _occupied = new(occupied.Select(SemanticCaptureIdentifier.Decode), StringComparer.Ordinal);

    internal string Allocate(string spelling)
    {
        var identity = SemanticCaptureIdentifier.Decode(spelling);
        if (_occupied.Add(identity)) return spelling;
        var suffix = 1;
        string candidate;
        do
        {
            candidate = suffix == 1 ? $"{identity}_" : $"{identity}_{suffix}";
            suffix++;
        }
        while (_reserved.Contains(candidate) || !_occupied.Add(candidate));
        return Identifiers.EscapeKeyword(candidate);
    }
}
