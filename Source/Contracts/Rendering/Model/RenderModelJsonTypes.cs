// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal static class RenderModelJsonTypes
{
    internal static void AddIdentityConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new RenderIdentityJsonConverter<SemanticId>(SemanticId.Parse, value => value.ToString()));
        options.Converters.Add(new RenderIdentityJsonConverter<SemanticRevision>(SemanticRevision.Parse, value => value.ToString()));
    }

    internal static void Configure(JsonTypeInfo info)
    {
        var derived = Variants(info.Type);
        if (derived.Length > 0)
        {
            info.PolymorphismOptions = new() { TypeDiscriminatorPropertyName = "$kind" };
            foreach (var type in derived)
            {
                info.PolymorphismOptions.DerivedTypes.Add(new(type, JsonNamingPolicy.CamelCase.ConvertName(type.Name)));
            }
        }
    }

    static Type[] Variants(Type type) => type switch
    {
        _ when type == typeof(RenderExpression) => [typeof(RenderExpression.Value), typeof(RenderExpression.Resolved), typeof(RenderExpression.EventContext), typeof(RenderExpression.Refusal), typeof(RenderExpression.ReadPath), typeof(RenderExpression.Comparison), typeof(RenderExpression.Logical)],
        _ when type == typeof(RenderLiteral) => [typeof(RenderLiteral.Null), typeof(RenderLiteral.Text), typeof(RenderLiteral.Number), typeof(RenderLiteral.Boolean), typeof(RenderLiteral.Array), typeof(RenderLiteral.Composite)],
        _ when type == typeof(RenderAuthorization) => [typeof(RenderAuthorization.Authenticated), typeof(RenderAuthorization.Anonymous), typeof(RenderAuthorization.Role), typeof(RenderAuthorization.Claim), typeof(RenderAuthorization.Policy), typeof(RenderAuthorization.OpaquePolicy), typeof(RenderAuthorization.NotPolicy), typeof(RenderAuthorization.Logical)],
        _ when type == typeof(RenderResponse) => [typeof(RenderResponse.Absent), typeof(RenderResponse.Scalar), typeof(RenderResponse.Record)],
        _ => []
    };
}

internal sealed class RenderIdentityJsonConverter<T>(Func<string, T> parse, Func<T, string> format) : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => parse(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(format(value));
}
