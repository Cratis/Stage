// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class JsonScriptValues
{
    internal static object? Convert(JsonElement element, int maxStringLength, int depth = 0)
    {
        if (depth > 64) throw Invalid("JSON depth exceeds 64.");
        return element.ValueKind switch
        {
            JsonValueKind.Object => Object(element, maxStringLength, depth),
            JsonValueKind.Array => Array(element, maxStringLength, depth),
            JsonValueKind.String => Text(element, maxStringLength),
            _ => Primitive(element)
        };
    }

    internal static TemplateRefusal Invalid(string message) => new(new(TemplateCodes.Input, message, string.Empty, 0, 0));

    static JsonObject Object(JsonElement element, int maxStringLength, int depth)
    {
        var data = new JsonObject(element.Clone());
        foreach (var property in element.EnumerateObject())
        {
            if (data.ContainsKey(property.Name)) throw Invalid("Duplicate JSON field.");
            CheckText(property.Name, maxStringLength);
            data.SetValue(property.Name, Convert(property.Value, maxStringLength, depth + 1), true);
        }

        data.IsReadOnly = true;
        return data;
    }

    static JsonArray Array(JsonElement element, int maxStringLength, int depth)
    {
        var array = new JsonArray(element.Clone());
        foreach (var item in element.EnumerateArray()) array.Add(Convert(item, maxStringLength, depth + 1));
        array.IsReadOnly = true;
        return array;
    }

    static string Text(JsonElement element, int maxStringLength)
    {
        var text = element.GetString()!;
        CheckText(text, maxStringLength);
        return text;
    }

    static object? Primitive(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => Number(element),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => throw Invalid("Undefined JSON is not an input value.")
    };

    static object Number(JsonElement element)
    {
        if (element.TryGetInt64(out var integer)) return integer;
        if (element.TryGetDecimal(out var number)) return number;
        throw Invalid("JSON number must fit in a decimal.");
    }

    static void CheckText(string text, int maxStringLength)
    {
        if (TemplatePreScan.HasPrivateUse(text)) throw new TemplateRefusal(new(TemplateCodes.JsonPrivateUse, "Private-use code point in ingress JSON.", string.Empty, 0, 0));
        if (text.Length > maxStringLength) throw Invalid("Ingress string exceeds the string limit.");
    }

    internal sealed class JsonObject(JsonElement source) : ScriptObject
    {
        internal JsonElement Source { get; } = source;
    }

    internal sealed class JsonArray(JsonElement source) : ScriptArray
    {
        internal JsonElement Source { get; } = source;
    }
}
