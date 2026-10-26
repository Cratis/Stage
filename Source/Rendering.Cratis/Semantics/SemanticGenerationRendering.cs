// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
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

    internal static bool IsRule(SemanticGeneration? generation) => string.Equals(generation?.Kind, "implementation", StringComparison.Ordinal);

    internal static bool UsesContext(SemanticGeneration? generation) => IsRule(generation) || IsTimeOrdered(generation) || SemanticPatternShape.HasDates(generation);

    internal static string BodyMethod(SemanticConcept concept)
    {
        var suffix = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(concept.Generation!.RequirementId!)))[..8];
        var method = $"Generate_{suffix}";

        return string.Equals(method, Naming.Identifiers.ToPascalCase(concept.Name), StringComparison.Ordinal) ? method + "_" : method;
    }

    internal static void RenderBody(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context)
    {
        if (!IsRule(concept.Generation)) return;
        var requirement = RequirementOf(concept, context);
        if (!ImplementationLanguage.IsCSharp(requirement))
        {
            // Another language is replaced, never emitted, and the warning names the authored language.
            var warning = ImplementationLanguage.Warning(requirement, requirement.Source.SemanticId, $"Generation for concept '{concept.Name}'");
            if (!context.InlineDiagnostics.Contains(warning)) context.InlineDiagnostics.Add(warning);
            builder.Using("Cratis.Concepts")
                .OpenBlock($"static string {BodyMethod(concept)}(global::Cratis.Concepts.GenerationContext context)")
                .Line(ImplementationLanguage.ThrowingBody())
                .EndBlock()
                .BlankLine();
            return;
        }

        var inline = SemanticInlineCodeRendering.Generation(concept, context);
        if (inline.Refused)
        {
            throw new InvalidArtifactRenderContract(string.Join(Environment.NewLine, inline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        // The raw body binds against this namespace in the compile gate, independent of import shortening.
        builder.Using("Cratis.Concepts")
            .OpenBlock($"static string {BodyMethod(concept)}(global::Cratis.Concepts.GenerationContext context)")
            .Inline(inline.Code!, inline.Placement, reportDegradation: diagnostic => context.InlineDiagnostics.Add(
                new(diagnostic.Code, ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Message, concept.Id)))
            .EndBlock()
            .BlankLine();
    }

    static SemanticImplementationRequirement RequirementOf(SemanticConcept concept, SemanticApplicationContext context) =>
        context.Request.ImplementationRequirements.Single(candidate => candidate.RequirementId == concept.Generation!.RequirementId);

    internal static string Factory(SemanticConcept concept, string? context, CSharpCodeBuilder? builder = null)
    {
        var generation = concept.Generation ?? new SemanticGeneration("uuid");
        if (IsRule(generation)) return $"{BodyMethod(concept)}({context})";
        var value = Strategy(generation, context, builder);

        return concept.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.WholeNumber ? value : Text(generation, value);
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
            "pattern" => $"{generator}.Pattern({Literal(generation.Pattern!)}{(context is null ? string.Empty : $", {context}")}{(generation.Checksum is null ? string.Empty : $", {Name(builder, "Checksum")}.{SemanticChecksumRendering.Algorithm(SemanticValue.Text(generation.Checksum))}")})",
            "random" => $"{generator}.Integer({generation.Minimum!.Value.ToString(CultureInfo.InvariantCulture)}L, {generation.Maximum!.Value.ToString(CultureInfo.InvariantCulture)}L)",
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
