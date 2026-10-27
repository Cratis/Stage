// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal static class TypeDescriptors
{
    internal static TypeSpelling Spell(object? value, ITemplateLanguage language, ITypeDirectory types)
    {
        if (value is not JsonScriptValues.JsonObject data) throw JsonScriptValues.Invalid("type.ref requires a JSON type reference.");
        var source = data.Source;
        OperatorTable.Fields(source, ["kind", "primitive", "target", "isCollection", "isOptional"], true);
        var kind = Text(source, "kind");
        var element = kind switch
        {
            "primitive" => language.Primitive(Text(source, "primitive")),
            "concept" or "composite" => Resolve(types, Text(source, "target")),
            _ => throw JsonScriptValues.Invalid("Unknown type kind.")
        };
        if (Flag(source, "isCollection")) element = language.Collection(element);
        if (Flag(source, "isOptional")) element = language.Optional(element);
        return element;
    }

    static TypeSpelling Resolve(ITypeDirectory types, string target) => types.TryGet(target, out var symbol)
        ? new([new TemplateSymbolSegment(TemplateSymbolKind.Reference, symbol)])
        : throw JsonScriptValues.Invalid("Unknown semantic type identity: " + target);

    static string Text(JsonElement source, string name) => source.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : throw JsonScriptValues.Invalid("Type reference requires " + name + ".");

    static bool Flag(JsonElement source, string name) => source.GetProperty(name).ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw JsonScriptValues.Invalid("Type reference requires Boolean " + name + ".")
    };
}
