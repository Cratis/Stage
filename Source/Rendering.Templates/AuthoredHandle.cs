// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal sealed class AuthoredHandle(TemplateAuthoredSegment segment)
{
    internal TemplateAuthoredSegment Segment { get; } = segment;

    public override string ToString() => throw SymbolHandle.NotText();

    internal static AuthoredHandle Create(object? requirementId)
    {
        if (requirementId is not string { Length: > 0 } id || TemplatePreScan.HasPrivateUse(id)) throw JsonScriptValues.Invalid("impl.body requires a nonempty requirement id without private-use code points.");
        return new(new(id));
    }
}
