// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Concepts;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Generates command values using the concept's admitted strategy.
/// </summary>
internal static class SemanticGeneratedValues
{
    internal static ImmutableArray<SemanticPropertyValue> For(SemanticCommand command, ExecutableSemanticModel model, DateTimeOffset now) =>
        [.. command.Properties.Where(property => property.IsGenerated).Select(property => new SemanticPropertyValue(
            property.Id, SemanticValue.Text(New(model.Application.Concepts.Single(concept => concept.Id == property.Type.Target), model.SemanticVersion, new GenerationContext(now, GenerationContext.DefaultTenant)))))];

    internal static string New(SemanticConcept concept, SemanticVersion version, GenerationContext context)
    {
        if (!version.IsAtLeast(SemanticVersion.V15)) return Guid.NewGuid().ToString();
        var generation = concept.Generation ?? new SemanticGeneration("uuid");
        var value = generation.Kind switch
        {
            "uuid" => GenerateValue.Uuid().ToString("D"),
            "uuidv7" => GenerateValue.UuidV7(context).ToString("D"),
            "ulid" => GenerateValue.Ulid(context),
            "typeid" => GenerateValue.TypeId(generation.Prefix ?? string.Empty, context),
            "nanoid" => GenerateValue.NanoId(generation.Size ?? 21, generation.Alphabet),
            "bytes" => GenerateValue.Bytes(generation.Count!.Value, Encoding(generation.Encoding)),
            _ => throw new UnsupportedSemanticValue($"Generation strategy '{generation.Kind}' is not admitted.")
        };
        value = generation.Case switch
        {
            "upper" => value.ToUpperInvariant(),
            "lower" => value.ToLowerInvariant(),
            _ => value
        };

        return generation.Kind == "typeid" ? value : $"{generation.Prefix}{value}";
    }

    static ValueEncoding Encoding(string? encoding) => encoding switch
    {
        "hex" => ValueEncoding.Hex,
        "base32" => ValueEncoding.Base32,
        "base64url" => ValueEncoding.Base64Url,
        _ => throw new UnsupportedSemanticValue($"Byte encoding '{encoding}' is not admitted.")
    };
}
