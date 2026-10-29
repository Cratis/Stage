// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal sealed class SymbolHandle(TemplateSymbolSegment segment)
{
    internal TemplateSymbolSegment Segment { get; } = segment;

    public override string ToString() => throw NotText();

    internal static TemplateRefusal NotText() => new(new(TemplateCodes.Symbol, "A symbol can only be emitted directly in a template body.", string.Empty, 0, 0));

    internal static SymbolHandle Create(TemplateSymbolKind kind, object? ns, object? name)
    {
        if (ns is not string { Length: > 0 } space || name is not string { Length: > 0 } shortName || TemplatePreScan.HasPrivateUse(space) || TemplatePreScan.HasPrivateUse(shortName)) throw JsonScriptValues.Invalid("Symbols require nonempty namespace and name without private-use code points.");
        return new(new(kind, new(space, shortName)));
    }
}
