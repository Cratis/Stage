// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;

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
            "pattern" => GenerateValue.Pattern(generation.Pattern!, context, generation.Checksum switch
            {
                "luhn" => Checksum.Luhn,
                "mod11" => Checksum.Mod11,
                "mod97" => Checksum.Mod97,
                _ => Checksum.None
            }),
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

    internal static SemanticExecutionResult ReportDefect(SemanticCommand? command, SemanticApplication application, SemanticExecutionResult result)
    {
        // Only the evaluator can decide that execution reached generation, after authorization and input checks.
        if (result is SemanticRejected { Category: SemanticRejectionCategory.Contract } rejected &&
            command?.Properties.Any(property => property.IsGenerated && IsGeneratedFailure(property, application, rejected.Details)) == true)
        {
            throw new SemanticCommandExecutionFailed(rejected.Details);
        }

        return result;
    }

    static SemanticPropertyValue Generate(SemanticProperty property, SemanticApplication application, DateTimeOffset now)
    {
        var concept = application.Concepts.Single(concept => concept.Id == property.Type.Target);
        var usesGeneration = SemanticGenerationShapes.UsesConceptGeneration(application);

        var value = concept.Generation is { } range && string.Equals(range.Kind, "random", StringComparison.Ordinal)
            ? SemanticValue.Number(GenerateValue.Integer(range.Minimum!.Value, range.Maximum!.Value))
            : SemanticValue.Text(New(concept, usesGeneration, new(now, GenerationContext.DefaultTenant)));

        return new(property.Id, value);
    }

    static bool IsGeneratedFailure(SemanticProperty property, SemanticApplication application, string message) =>
        string.Equals(message, SemanticGenerationShapes.FixtureMessage(property.Name), StringComparison.Ordinal) ||
        string.Equals(message, SemanticGenerationShapes.RuleMessage(property.Name, application.Concepts.Single(concept => concept.Id == property.Type.Target).Name), StringComparison.Ordinal);

    static ValueEncoding Encoding(string? encoding) => encoding switch
    {
        "hex" => ValueEncoding.Hex,
        "base32" => ValueEncoding.Base32,
        "base64url" => ValueEncoding.Base64Url,
        _ => throw new UnsupportedSemanticValue($"Byte encoding '{encoding}' is not admitted.")
    };
}
