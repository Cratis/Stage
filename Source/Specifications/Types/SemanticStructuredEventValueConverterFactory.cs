// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Converts <see cref="SemanticStructuredEventValue"/> subclasses to and from their raw JSON value.
/// </summary>
public sealed class SemanticStructuredEventValueConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeof(SemanticStructuredEventValue).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    sealed class Converter<T> : JsonConverter<T>
        where T : SemanticStructuredEventValue, new()
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new() { Value = JsonElement.ParseValue(ref reader) };

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            value.Value.WriteTo(writer);
    }
}
