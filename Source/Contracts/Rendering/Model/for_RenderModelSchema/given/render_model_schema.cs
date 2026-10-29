// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelSchema.given;

internal static class render_model_schema
{
    internal static byte[] Generate()
    {
        // The sequence converter only enables construction on read. Export its identical built-in
        // IEnumerable array contract, so recursive element references remain exporter-owned.
        var options = new JsonSerializerOptions(RenderModelJson.Options);
        options.Converters.Remove(options.Converters.OfType<RenderSequenceJsonConverter>().Single());
        options.Converters.Remove(options.Converters.OfType<RenderDecimalJsonConverter>().Single());
        var schema = options.GetJsonSchemaAsNode(typeof(RenderModel), new() { TransformSchemaNode = Transform, TreatNullObliviousAsNonNullable = true });
        schema["$id"] = "https://schemas.cratis.io/stage/render-model/v1.json";

        return RenderModelJson.CanonicalBytes(JsonSerializer.SerializeToElement(schema));
    }

    internal static string FilePath(string relativePath) => Path.Combine(ModelDirectory(), relativePath);

    static string ModelDirectory([System.Runtime.CompilerServices.CallerFilePath] string file = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../.."));

    static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node)
    {
        var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
        if (type == typeof(SemanticId) || type == typeof(SemanticRevision))
        {
            return new JsonObject { ["type"] = Nullable.GetUnderlyingType(context.TypeInfo.Type) is null ? JsonValue.Create("string") : new JsonArray("string", "null") };
        }
        if (node is JsonObject obj && obj["properties"] is JsonObject properties)
        {
            obj["additionalProperties"] = false;
            obj["required"] = new JsonArray([.. properties.Select(property => (JsonNode?)JsonValue.Create(property.Key))]);
            ApplyConstants(type, properties);
        }

        TightenGetterNullability(context, node);

        return node;
    }

    static void TightenGetterNullability(JsonSchemaExporterContext context, JsonNode node)
    {
        // The exporter also considers a missing setter nullable; the wire contract follows
        // the annotated getter for computed members such as Kind and Provenance.
        if (context.PropertyInfo?.IsGetNullable != false) return;
        if (node is not JsonObject obj) return;
        if (obj["type"] is not JsonArray types) return;
        for (var index = types.Count - 1; index >= 0; index--)
        {
            if (types[index]!.GetValue<string>() == "null") types.RemoveAt(index);
        }
        if (types.Count == 1) obj["type"] = types[0]!.DeepClone();
    }

    static void ApplyConstants(Type type, JsonObject properties)
    {
        if (type == typeof(RenderCommand)) properties["kind"]!["const"] = "command";
        if (type == typeof(RenderReaction)) properties["kind"]!["const"] = "reaction";
        if (type == typeof(RenderProvenance)) properties["status"]!["const"] = RenderProvenance.Absent.Status;
        if (type == typeof(RenderModel))
        {
            properties["schema"]!["const"] = RenderModel.Schema;
            properties["canonicalization"]!["const"] = 1;
            properties["minor"]!["const"] = 0;
        }
    }
}
