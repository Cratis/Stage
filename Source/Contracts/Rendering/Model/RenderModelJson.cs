// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Stage.Contracts.Rendering.Model;

/// <summary>
/// Serializes canonical form v1: UTF-8 without BOM, indentation or a trailing newline.
/// Record properties retain declaration order; dictionary keys sort ordinally. Builder collections
/// are already ordered by identity; semantic-order sequences are never reordered here.
/// Numbers use shortest non-exponent invariant forms, with integers having no decimal
/// point. Strings use UnsafeRelaxedJsonEscaping, except all control characters use \uXXXX.
/// The schema, minor and canonicalization versions are independent document fields.
/// </summary>
internal static class RenderModelJson
{
    internal static JsonSerializerOptions Options { get; } = CreateOptions();

    internal static byte[] Serialize<T>(T value) => CanonicalBytes(JsonSerializer.SerializeToElement(value, Options));

    internal static RenderModel Deserialize(ReadOnlySpan<byte> json) => JsonSerializer.Deserialize<RenderModel>(json, Options)!;

    internal static byte[] CanonicalBytes(JsonElement value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        value.WriteTo(writer);
        writer.Flush();

        return System.Text.Encoding.UTF8.GetBytes(NormalizeEscapes(System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan)));
    }

    static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(RenderModelJsonTypes.Configure);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false), new RenderSequenceJsonConverter(), new RenderDictionaryJsonConverter(), new RenderDecimalJsonConverter() }
        };
        RenderModelJsonTypes.AddIdentityConverters(options);
        options.MakeReadOnly();

        return options;
    }

    static string NormalizeEscapes(string encoded)
    {
        var result = new System.Text.StringBuilder(encoded.Length);
        for (var index = 0; index < encoded.Length; index++)
        {
            var character = encoded[index];
            if (character != '\\')
            {
                result.Append(character);
                continue;
            }
            var escaped = encoded[++index];
            result.Append(escaped switch
            {
                'b' => "\\u0008",
                't' => "\\u0009",
                'n' => "\\u000A",
                'f' => "\\u000C",
                'r' => "\\u000D",
                _ => $"\\{escaped}"
            });
        }

        return result.ToString();
    }
}

internal sealed class RenderDecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetDecimal();
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteRawValue(value.ToString("0.############################", CultureInfo.InvariantCulture));
}
