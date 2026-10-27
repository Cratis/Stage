// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Catalogues java@1 integrity refusals. These codes belong to STAGE-JAVA, never
/// STAGE-DEGRADE; semantic degradation diagnostics are owned by the planner.
/// </summary>
public static class JavaRefusalCodes
{
    /// <summary>
    /// STAGE-JAVA-0001: an identifier contains '$' or cannot satisfy the java@1 identifier policy.
    /// </summary>
    public const string InvalidIdentifier = "STAGE-JAVA-0001";

    /// <summary>
    /// STAGE-JAVA-0002: an eligible JLS 3.3 Unicode escape lacks four hexadecimal digits.
    /// </summary>
    public const string MalformedUnicodeEscape = "STAGE-JAVA-0002";

    /// <summary>
    /// STAGE-JAVA-0003: raw input or complete output, or its JLS 3.3 translation, contains a private-use code point.
    /// </summary>
    public const string PrivateUseCodePoint = "STAGE-JAVA-0003";

    /// <summary>
    /// STAGE-JAVA-0004: raw input or complete output, or its JLS 3.3 translation, contains a lone UTF-16 surrogate.
    /// </summary>
    public const string LoneSurrogate = "STAGE-JAVA-0004";

    /// <summary>
    /// STAGE-JAVA-0005: a gap or native segment has a context other than type, member, or statement.
    /// </summary>
    public const string UnsupportedContext = "STAGE-JAVA-0005";

    /// <summary>
    /// STAGE-JAVA-0006: a Text/Native span ends in a raw or decoded backslash when any segment follows it.
    /// </summary>
    public const string UnsafeLiteralBoundary = "STAGE-JAVA-0006";

    /// <summary>
    /// STAGE-JAVA-0007: a requested declaration or enclosing declaration is missing.
    /// </summary>
    public const string MissingDeclaration = "STAGE-JAVA-0007";

    /// <summary>
    /// STAGE-JAVA-0008: a declaration identity or inferred enclosing declaration is ambiguous.
    /// </summary>
    public const string AmbiguousDeclaration = "STAGE-JAVA-0008";

    /// <summary>
    /// STAGE-JAVA-0009: a native Java block comment, text block, string, or character literal is unterminated.
    /// </summary>
    public const string UnterminatedNativeConstruct = "STAGE-JAVA-0009";

    /// <summary>
    /// STAGE-JAVA-0010: no simple or qualified type spelling has a proven binding in the file scope.
    /// </summary>
    public const string ObscuredReference = "STAGE-JAVA-0010";

    /// <summary>
    /// STAGE-JAVA-0011: a declaration's explicit scope disagrees with its structured type.
    /// </summary>
    public const string InvalidDeclarationScope = "STAGE-JAVA-0011";

    /// <summary>
    /// STAGE-JAVA-0012: packages or their directory prefixes differ only by ordinal case.
    /// </summary>
    public const string CaseConflictingPackage = "STAGE-JAVA-0012";

    /// <summary>
    /// STAGE-JAVA-0013: generated Java code declares a type without a structured DeclarationSegment.
    /// </summary>
    public const string UnstructuredDeclaration = "STAGE-JAVA-0013";

    /// <summary>STAGE-JAVA-0014: emitted structure or a symbol's code-token placement is unproven.</summary>
    public const string UnprovenStructure = "STAGE-JAVA-0014";

    /// <summary>STAGE-JAVA-0015: a qualified reference or placeholder lacks a proven unobscured package prefix.</summary>
    public const string UnprovenQualifiedBinding = "STAGE-JAVA-0015";

    /// <summary>STAGE-JAVA-0016: a gap lacks a non-empty STAGE-DEGRADE suffix or uses #807's reserved 001–005 range.</summary>
    public const string InvalidGapCode = "STAGE-JAVA-0016";
}
