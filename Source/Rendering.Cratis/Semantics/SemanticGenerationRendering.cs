// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the admitted Fundamentals strategy factories without executing them.
/// </summary>
internal static class SemanticGenerationRendering
{
    internal static bool IsTimeOrdered(SemanticGeneration? generation) =>
        string.Equals(generation?.Kind, "uuidv7", StringComparison.Ordinal) ||
        string.Equals(generation?.Kind, "ulid", StringComparison.Ordinal) ||
        string.Equals(generation?.Kind, "typeid", StringComparison.Ordinal);

    internal static string Factory(SemanticConcept concept, string? context, CSharpCodeBuilder? builder = null)
    {
        var generation = concept.Generation ?? new SemanticGeneration("uuid");
        var value = Strategy(generation, context, builder);

        return concept.Primitive == SemanticPrimitiveType.Uuid ? value : Text(generation, value);
    }

    static string Strategy(SemanticGeneration generation, string? context, CSharpCodeBuilder? builder)
    {
        var generator = Name(builder, "GenerateValue");
        return generation.Kind switch
        {
            "uuid" => $"{generator}.Uuid()",
            "uuidv7" => $"{generator}.UuidV7({context})",
            "ulid" => $"{generator}.Ulid({context})",
            "typeid" => $"{generator}.TypeId({Literal(generation.Prefix ?? string.Empty)}, {context})",
            "nanoid" => $"{generator}.NanoId({Number(generation.Size ?? 21)}, {(generation.Alphabet is { } alphabet ? Literal(alphabet) : "null")})",
            "bytes" => $"{generator}.Bytes({Number(generation.Count!.Value)}, {Name(builder, "ValueEncoding")}.{Encoding(generation.Encoding)})",
            _ => throw new InvalidArtifactRenderContract($"Generation strategy '{generation.Kind}' is not admitted.")
        };
    }

    static string Name(CSharpCodeBuilder? builder, string name)
    {
        var reference = new TypeReference("Cratis.Concepts", name, 0, ReferenceContext.Expression);
        return builder?.Type(reference) ?? reference.Qualified;
    }

    static string Text(SemanticGeneration generation, string value)
    {
        if (string.Equals(generation.Kind, "uuid", StringComparison.Ordinal) || string.Equals(generation.Kind, "uuidv7", StringComparison.Ordinal)) value += ".ToString(\"D\")";
        value += generation.Case switch
        {
            "upper" => ".ToUpperInvariant()",
            "lower" => ".ToLowerInvariant()",
            _ => string.Empty
        };

        return generation.Kind != "typeid" && generation.Prefix is { Length: > 0 } prefix ? $"{Literal(prefix)} + {value}" : value;
    }

    static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    static string Literal(string value) => CSharpCodeBuilder.StringLiteral(value);
    static string Encoding(string? encoding) => encoding switch
    {
        "hex" => "Hex",
        "base32" => "Base32",
        "base64url" => "Base64Url",
        _ => throw new InvalidArtifactRenderContract($"Byte encoding '{encoding}' is not admitted.")
    };
}
