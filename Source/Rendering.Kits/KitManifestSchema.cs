// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Generates the manifest schema directly from the strict serialization contract.</summary>
public static class KitManifestSchema
{
    /// <summary>Generates the stage-target/v1 schema with a stable identity and closed objects.</summary>
    /// <returns>The indented JSON schema, with a final newline.</returns>
    public static string Generate()
    {
        var schema = KitJson.Options.GetJsonSchemaAsNode(typeof(KitManifest), new JsonSchemaExporterOptions { TransformSchemaNode = Transform });
        schema["$schema"] = "https://json-schema.org/draft/2020-12/schema";
        schema["$id"] = "https://cratis.io/schemas/stage-target.v1.schema.json";
        schema["title"] = "Stage target kit v1";
        schema["$comment"] = "Copyright (c) Cratis. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.";
        return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (context.TypeInfo.Type == typeof(ContractVersion) || context.TypeInfo.Type == typeof(LanguagePackVersion))
        {
            schema = new JsonObject
            {
                ["type"] = "string",
                ["pattern"] = context.TypeInfo.Type == typeof(ContractVersion) ? ContractVersion.Pattern : LanguagePackVersion.Pattern
            };
        }
        else if (context.TypeInfo.Kind == JsonTypeInfoKind.Object && schema is JsonObject)
        {
            schema["additionalProperties"] = false;
        }

        var pattern = context.PropertyInfo?.AttributeProvider?.GetCustomAttributes(typeof(KitPatternAttribute), true).OfType<KitPatternAttribute>().FirstOrDefault();
        if (pattern is not null)
        {
            schema["pattern"] = pattern.Pattern;
        }

        return schema;
    }
}
