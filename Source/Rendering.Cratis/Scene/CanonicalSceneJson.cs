// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Serializes a translated <see cref="SceneApplication"/> to canonical JSON.
/// </summary>
/// <remarks>
/// The emitted payload is a planned artifact, so two serializations of the same application have to be
/// byte-identical or the whole determinism guarantee is void. Serializer member order is a property of the
/// current type shape rather than a promise, so the object graph is re-emitted here with every property name
/// ordered ordinally, invariant formatting, and no indentation. Nothing about the payload depends on the host
/// culture, the clock, or the order the translation happened to visit things in.
/// </remarks>
public static class CanonicalSceneJson
{
    static readonly JsonSerializerOptions _readOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    static readonly JsonWriterOptions _writeOptions = new()
    {
        Indented = false,
        SkipValidation = false
    };

    /// <summary>
    /// Serializes the translated application to canonical JSON.
    /// </summary>
    /// <param name="application">The translated Scene application.</param>
    /// <returns>The canonical JSON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="application"/> is <c language="csharp">null</c>.</exception>
    public static string Serialize(SceneApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var node = JsonSerializer.SerializeToNode(application, _readOptions) ?? new JsonObject();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, _writeOptions))
        {
            Write(node, writer);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    static void Write(JsonNode? node, Utf8JsonWriter writer)
    {
        switch (node)
        {
            case JsonObject o:
                writer.WriteStartObject();
                foreach (var property in o.OrderBy(_ => _.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Key);
                    Write(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonArray a:
                writer.WriteStartArray();
                foreach (var item in a)
                {
                    Write(item, writer);
                }

                writer.WriteEndArray();
                break;

            case null:
                writer.WriteNullValue();
                break;

            default:
                node.WriteTo(writer);
                break;
        }
    }
}
