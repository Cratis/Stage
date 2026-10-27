// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class SymbolHandle(SymbolDescriptor descriptor)
{
    internal SymbolDescriptor Descriptor { get; } = descriptor;

    public override string ToString() => throw NotText();

    internal static TemplateRefusal NotText() => new(new(TemplateCodes.Symbol, "A symbol can only be emitted directly in a template body.", string.Empty, 0, 0));

    internal static SymbolDescriptor Parse(object? value)
    {
        if (value is not JsonScriptValues.JsonObject data || !data.TryGetValue("$symbol", out var marker) || marker is not true || !data.TryGetValue("identity", out var id) || id is not string identity || string.IsNullOrWhiteSpace(identity))
        {
            throw JsonScriptValues.Invalid("Expected a tagged symbol descriptor with $symbol:true and identity.");
        }

        if (data.Keys.Any(key => key is not ("$symbol" or "identity" or "namespace" or "name"))) throw JsonScriptValues.Invalid("Unknown symbol descriptor field.");
        return new(identity, Optional(data, "namespace"), Optional(data, "name"));
    }

    static string? Optional(ScriptObject value, string name)
    {
        if (!value.TryGetValue(name, out var field) || field is null) return null;
        return field as string ?? throw JsonScriptValues.Invalid("Symbol fields must be text or null.");
    }
}
