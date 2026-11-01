// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal sealed class RenderSequenceJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(RenderSequence<>);
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => (JsonConverter)Activator.CreateInstance(typeof(SequenceConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;

    sealed class SequenceConverter<T> : JsonConverter<RenderSequence<T>>
    {
        public override RenderSequence<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(JsonSerializer.Deserialize<T[]>(ref reader, options)!);
        public override void Write(Utf8JsonWriter writer, RenderSequence<T> value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.ToArray(), options);
    }
}

internal sealed class RenderDictionaryJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.GetInterfaces().Append(typeToConvert).Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) && type.GetGenericArguments()[0] == typeof(string));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var contract = typeToConvert.GetInterfaces().Append(typeToConvert).First(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

        return (JsonConverter)Activator.CreateInstance(typeof(DictionaryConverter<,>).MakeGenericType(typeToConvert, contract.GetGenericArguments()[1]), args: [options])!;
    }

    sealed class DictionaryConverter<TDictionary, TValue>(JsonSerializerOptions writeOptions) : JsonConverter<TDictionary>
        where TDictionary : IReadOnlyDictionary<string, TValue>
    {
        readonly JsonSerializerOptions _readOptions = ReadOptions(writeOptions);

        public override TDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<TDictionary>(ref reader, _readOptions)!;

        public override void Write(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var pair in value.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(pair.Key);
                JsonSerializer.Serialize(writer, pair.Value, options);
            }
            writer.WriteEndObject();
        }

        static JsonSerializerOptions ReadOptions(JsonSerializerOptions options)
        {
            var result = new JsonSerializerOptions(options);
            result.Converters.Remove(result.Converters.OfType<RenderDictionaryJsonConverter>().Single());

            return result;
        }
    }
}
