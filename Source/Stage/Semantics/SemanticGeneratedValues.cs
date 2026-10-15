// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Generates command values using the concept's admitted strategy.
/// </summary>
internal static class SemanticGeneratedValues
{
    internal static ImmutableArray<SemanticPropertyValue> For(SemanticCommand command, ExecutableSemanticModel model, DateTimeOffset now) =>
        [.. command.Properties.Where(property => property.IsGenerated).Select(property => Generate(property, model.Application, now))];

    internal static string New(SemanticConcept concept, bool usesConceptGeneration, GenerationContext context)
    {
        if (!usesConceptGeneration) return Guid.NewGuid().ToString();
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

    static SemanticPropertyValue Generate(SemanticProperty property, SemanticApplication application, DateTimeOffset now)
    {
        var concept = application.Concepts.Single(concept => concept.Id == property.Type.Target);
        var usesGeneration = SemanticGenerationShapes.UsesConceptGeneration(application);
        var value = SemanticValue.Text(New(concept, usesGeneration, new(now, GenerationContext.DefaultTenant)));
        if ((usesGeneration || !concept.Validations.IsEmpty) && !SemanticGenerationShapes.Matches(concept, value))
        {
            throw new SemanticCommandExecutionFailed(SemanticGenerationShapes.FixtureMessage(property.Name));
        }
        if (SemanticRuleValues.GeneratedFailure(concept, property.Name, value) is { } failure)
        {
            throw new SemanticCommandExecutionFailed(failure);
        }

        return new(property.Id, value);
    }

    static ValueEncoding Encoding(string? encoding) => encoding switch
    {
        "hex" => ValueEncoding.Hex,
        "base32" => ValueEncoding.Base32,
        "base64url" => ValueEncoding.Base64Url,
        _ => throw new UnsupportedSemanticValue($"Byte encoding '{encoding}' is not admitted.")
    };
}
