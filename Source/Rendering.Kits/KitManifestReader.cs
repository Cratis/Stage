// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Stage.Rendering.Kits;

static class KitManifestReader
{
    internal static KitManifest? Read(ImmutableArray<byte> bytes, List<KitDiagnostic> diagnostics)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.AsMemory(), new JsonDocumentOptions { MaxDepth = 32 });
            var initialCount = diagnostics.Count;
            Inspect(document.RootElement, typeof(KitManifest), "$", diagnostics);
            if (diagnostics.Count != initialCount)
            {
                return null;
            }

            return JsonSerializer.Deserialize<KitManifest>(bytes.AsSpan(), KitJson.Options) ?? throw new JsonException("The manifest must be an object.");
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new(KitCodes.InvalidManifest, exception.Message, exception.Path ?? "$"));
            return null;
        }
        catch (InvalidOperationException exception)
        {
            // System.Text.Json reports undecodable text, such as a lone surrogate escape in a property name, this way.
            diagnostics.Add(new(KitCodes.InvalidManifest, exception.Message, "$"));
            return null;
        }
    }

    static void Inspect(JsonElement element, Type type, string path, List<KitDiagnostic> diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            diagnostics.Add(new(KitCodes.InvalidManifest, "Required values and array elements cannot be null.", path));
            return;
        }

        var info = KitJson.Options.GetTypeInfo(type);
        if (info.Kind == JsonTypeInfoKind.Object && element.ValueKind == JsonValueKind.Object)
        {
            InspectObject(element, info, path, diagnostics);
        }
        else if (info.Kind == JsonTypeInfoKind.Enumerable && element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                Inspect(item, type.GetGenericArguments()[0], $"{path}[{index++}]", diagnostics);
            }
        }
    }

    static void InspectObject(JsonElement element, JsonTypeInfo info, string path, List<KitDiagnostic> diagnostics)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in element.EnumerateObject())
        {
            var memberPath = $"{path}.{member.Name}";
            if (!names.Add(member.Name))
            {
                diagnostics.Add(new(KitCodes.InvalidManifest, "Duplicate JSON property.", memberPath));
            }

            var property = info.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, member.Name, StringComparison.Ordinal));
            if (property is null)
            {
                diagnostics.Add(new(KitCodes.UnknownMember, "Unknown manifest member.", memberPath));
            }
            else if (member.Value.ValueKind != JsonValueKind.Null || !property.IsGetNullable)
            {
                Inspect(member.Value, property.PropertyType, memberPath, diagnostics);
            }
        }
    }
}
