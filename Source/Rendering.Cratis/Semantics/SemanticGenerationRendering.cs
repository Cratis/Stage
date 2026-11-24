// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    internal static string BodyMethod(SemanticConcept concept, SemanticApplicationContext context) => context.InlineNames.GenerationBody(concept);

    internal static bool RenderFactoryBody(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context, string declaration)
    {
        if (!IsRule(concept.Generation)) return false;
        var inline = VerifiedBody(concept, context);
        if (!CanInline(inline.Code!)) return false;

        // The forwarding string conversion preserves the verified body, including compound returns, verbatim.
        builder.Using("Cratis.Concepts").OpenBlock(declaration);
        SpliceBody(builder, inline, context, concept);
        builder.EndBlock().BlankLine();
        return true;
    }

    internal static void RenderBody(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context)
    {
        if (!IsRule(concept.Generation)) return;
        var inline = VerifiedBody(concept, context);

        // The raw body binds against this namespace in the compile gate, independent of import shortening.
        builder.Using("Cratis.Concepts")
            .OpenBlock($"static string {BodyMethod(concept, context)}(global::Cratis.Concepts.GenerationContext context)");
        SpliceBody(builder, inline, context, concept);
        builder.EndBlock().BlankLine();
    }

    internal static string Factory(SemanticConcept concept, string? context, SemanticApplicationContext application, CSharpCodeBuilder? builder = null)
    {
        var generation = concept.Generation ?? new SemanticGeneration("uuid");
        if (IsRule(generation)) return $"{BodyMethod(concept, application)}({context})";
        if (IsUpperHex(generation)) return Prefixed(generation, UpperHex(generation, builder));
        var value = Strategy(generation, context, builder);

        return concept.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.WholeNumber ? value : Text(generation, value);
    }

    static bool IsUpperHex(SemanticGeneration generation) =>
        string.Equals(generation.Kind, "bytes", StringComparison.Ordinal) &&
        string.Equals(generation.Encoding, "hex", StringComparison.Ordinal) &&
        string.Equals(generation.Case, "upper", StringComparison.Ordinal);

    static SemanticInlineCodeResult VerifiedBody(SemanticConcept concept, SemanticApplicationContext context)
    {
        var inline = SemanticInlineCodeRendering.Generation(concept, context);
        if (inline.Refused)
        {
            throw new InvalidArtifactRenderContract(string.Join(Environment.NewLine, inline.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return inline;
    }

    static void SpliceBody(CSharpCodeBuilder builder, SemanticInlineCodeResult inline, SemanticApplicationContext context, SemanticConcept concept) =>
        builder.Inline(context.RetainInlineInput(inline), inline.Placement, reportDegradation: diagnostic => context.InlineDiagnostics.Add(
            new(diagnostic.Code, ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Message, concept.Id)));

    static bool CanInline(InlineCode code)
    {
        var body = SyntaxFactory.ParseStatement("{\n" + code.Text + "\n}");
        return !body.ContainsDirectives && !body.DescendantNodes().Any(RequiresStringHelper);
    }

    static bool RequiresStringHelper(SyntaxNode node) => node switch
    {
        AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => true,
        IdentifierNameSyntax name => name.Identifier.ValueText == "dynamic",
        ReturnStatementSyntax { Expression: { } expression } => !IsStringExpression(expression),
        _ => false
    };

    static bool IsStringExpression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression),
        InterpolatedStringExpressionSyntax => true,
        ParenthesizedExpressionSyntax parenthesized => IsStringExpression(parenthesized.Expression),
        BinaryExpressionSyntax binary => IsStringConcatenation(binary),
        InvocationExpressionSyntax invocation => IsStringInvocation(invocation),
        _ => false
    };

    static bool IsStringConcatenation(BinaryExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.AddExpression) && IsStringExpression(expression.Left) && IsStringExpression(expression.Right);

    static bool IsStringInvocation(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        IdentifierNameSyntax { Identifier.ValueText: "nameof" } => invocation.ArgumentList.Arguments.Count == 1,
        MemberAccessExpressionSyntax member => IsStringMemberInvocation(invocation, member),
        _ => false
    };

    static bool IsStringMemberInvocation(InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax member)
    {
        if (member.Expression is PredefinedTypeSyntax type && type.Keyword.IsKind(SyntaxKind.StringKeyword)) return IsStringFactory(member.Name);
        return member.Name is IdentifierNameSyntax toString && IsText(toString.Identifier.ValueText, "ToString") && invocation.ArgumentList.Arguments.Count == 0 && IsKnownToStringReceiver(member.Expression);
    }

    static bool IsStringFactory(SimpleNameSyntax name) => name is IdentifierNameSyntax &&
        Array.Exists(StringFactories(), factory => IsText(name.Identifier.ValueText, factory));

    static string[] StringFactories() => ["Concat", "Create", "Format", "Intern", "Join"];

    static bool IsText(string actual, string expected) => string.Equals(actual, expected, StringComparison.Ordinal);

    static bool IsKnownToStringReceiver(ExpressionSyntax expression) =>
        IsStringExpression(expression) || IsPrimitiveLiteral(expression) || IsContextTenant(expression);

    static bool IsPrimitiveLiteral(ExpressionSyntax expression) => expression.Kind() is
        SyntaxKind.NumericLiteralExpression or SyntaxKind.CharacterLiteralExpression or SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression;

    static bool IsContextTenant(ExpressionSyntax expression) => expression is MemberAccessExpressionSyntax
    {
        Expression: IdentifierNameSyntax receiver,
        Name: IdentifierNameSyntax property
    } && IsText(receiver.Identifier.ValueText, "context") && IsText(property.Identifier.ValueText, "Tenant");

    static string UpperHex(SemanticGeneration generation, CSharpCodeBuilder? builder)
    {
        var generator = builder?.Type(new("System.Security.Cryptography", "RandomNumberGenerator", 0, ReferenceContext.Expression)) ?? "global::System.Security.Cryptography.RandomNumberGenerator";
        return $"{generator}.GetHexString({Number(2 * generation.Count!.Value)}, false)";
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
            _ => SizedStrategy(generation, context, builder)
        };
    }

    static string SizedStrategy(SemanticGeneration generation, string? context, CSharpCodeBuilder? builder)
    {
        var generator = Name(builder, "GenerateValue");
        return generation.Kind switch
        {
            "nanoid" => $"{generator}.NanoId({Number(generation.Size ?? 21)}, {(generation.Alphabet is { } alphabet ? Literal(alphabet) : "null")})",
            "bytes" => $"{generator}.Bytes({Number(generation.Count!.Value)}, {Name(builder, "ValueEncoding")}.{Encoding(generation.Encoding)})",
            "pattern" => Pattern(generation, context, builder),
            "random" => $"{generator}.Integer({generation.Minimum!.Value.ToString(CultureInfo.InvariantCulture)}L, {generation.Maximum!.Value.ToString(CultureInfo.InvariantCulture)}L)",
            _ => throw new InvalidArtifactRenderContract($"Generation strategy '{generation.Kind}' is not admitted.")
        };
    }

    static string Pattern(SemanticGeneration generation, string? context, CSharpCodeBuilder? builder) =>
        $"{Name(builder, "GenerateValue")}.Pattern({Literal(generation.Pattern!)}{(context is null ? string.Empty : $", {context}")}{(generation.Checksum is null ? string.Empty : $", {Name(builder, "Checksum")}.{SemanticChecksumRendering.Algorithm(SemanticValue.Text(generation.Checksum))}")})";

    static string Name(CSharpCodeBuilder? builder, string name)
    {
        var reference = new TypeReference("Cratis.Concepts", name, 0, ReferenceContext.Expression);
        return builder?.Type(reference) ?? reference.Qualified;
    }

    static string Text(SemanticGeneration generation, string value)
    {
        if (string.Equals(generation.Kind, "uuid", StringComparison.Ordinal) || string.Equals(generation.Kind, "uuidv7", StringComparison.Ordinal)) value += ".ToString(\"D\")";
        return Prefixed(generation, Cased(generation.Case, value));
    }

    static string Cased(string? letterCase, string value) => letterCase switch
    {
        "upper" => value + ".ToUpperInvariant()",
        "lower" => value + ".ToLowerInvariant()",
        _ => value
    };

    static string Prefixed(SemanticGeneration generation, string value) =>
        generation.Kind != "typeid" && generation.Prefix is { Length: > 0 } prefix ? $"{Literal(prefix)} + {value}" : value;

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
