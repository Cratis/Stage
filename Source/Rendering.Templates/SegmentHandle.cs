// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class SegmentHandle(ImmutableArray<TemplateSegment> segments)
{
    internal ImmutableArray<TemplateSegment> Segments { get; } = segments;

    public override string ToString() => throw SymbolHandle.NotText();
}
