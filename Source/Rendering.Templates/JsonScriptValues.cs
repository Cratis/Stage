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
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var data = new JsonObject(element.Clone());
                foreach (var property in element.EnumerateObject())
                {
                    CheckText(property.Name, maxStringLength);
                    data.SetValue(property.Name, Convert(property.Value, maxStringLength, depth + 1), true);
                }

                data.IsReadOnly = true;
                return data;
            case JsonValueKind.Array:
                var array = new JsonArray(element.Clone());
                foreach (var item in element.EnumerateArray()) array.Add(Convert(item, maxStringLength, depth + 1));
                array.IsReadOnly = true;
                return array;
            case JsonValueKind.String:
                var text = element.GetString()!;
                CheckText(text, maxStringLength);
                return text;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var integer)) return integer;
                if (element.TryGetDecimal(out var number)) return number;
                throw Invalid("JSON number must fit in a decimal.");
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            default: throw Invalid("Undefined JSON is not an input value.");
        }
    }

    internal static TemplateRefusal Invalid(string message) => new(new(TemplateCodes.Input, message, string.Empty, 0, 0));

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
