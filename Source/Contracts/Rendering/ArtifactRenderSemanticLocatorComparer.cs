// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

sealed class ArtifactRenderSemanticLocatorComparer : IComparer<SemanticAddress>
{
    internal static readonly ArtifactRenderSemanticLocatorComparer Instance = new();

    public int Compare(SemanticAddress? x, SemanticAddress? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        var kind = StringComparer.Ordinal.Compare(x.Kind.ToString(), y.Kind.ToString());
        if (kind != 0) return kind;
        for (var index = 0; index < Math.Min(x.Parts.Length, y.Parts.Length); index++)
        {
            var part = ComparePart(x.Parts[index], y.Parts[index]);
            if (part != 0) return part;
        }
        return x.Parts.Length.CompareTo(y.Parts.Length);
    }

    static int ComparePart(SemanticAddressPart left, SemanticAddressPart right)
    {
        var kind = StringComparer.Ordinal.Compare(left.Kind.ToString(), right.Kind.ToString());
        return kind == 0 ? StringComparer.Ordinal.Compare(left.Key, right.Key) : kind;
    }
}
