// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal static class TypeDescriptors
{
    internal static TemplateType Parse(object? value)
    {
        if (value is not JsonScriptValues.JsonObject data) throw JsonScriptValues.Invalid("type.ref requires a JSON type descriptor.");
        return ParseType(data.Source, 0);
    }

    static TemplateType ParseType(JsonElement value, int depth)
    {
        if (depth >= 64) throw PlanMeter.Exceeded(TemplateCodes.Recursion, "Type descriptor nesting exceeded.");
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String) throw JsonScriptValues.Invalid("Type descriptor requires kind.");
        if (value.EnumerateObject().Any(property => property.Name is not ("kind" or "element" or "optional"))) throw JsonScriptValues.Invalid("Unknown type descriptor field.");
        var optional = value.TryGetProperty("optional", out var flag) && flag.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => throw JsonScriptValues.Invalid("optional must be Boolean.") };
        var element = value.TryGetProperty("element", out var item) ? ParseType(item, depth + 1) : null;
        if (kind.GetString() == "array" && element is null) throw JsonScriptValues.Invalid("Array type requires an element.");
        return new(kind.GetString()!, element, optional);
    }
}
