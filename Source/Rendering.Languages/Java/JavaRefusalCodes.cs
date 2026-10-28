// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Catalogues java@1 refusals. These codes belong to STAGE-JAVA, never
/// STAGE-DEGRADE; semantic degradation diagnostics are owned by the planner.
/// </summary>
public static class JavaRefusalCodes
{
    /// <summary>
    /// STAGE-JAVA-0001: an identifier cannot satisfy the java@1 identifier policy.
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
    /// STAGE-JAVA-0005: a declaration or imports symbol appears in a file that is not Java source.
    /// </summary>
    public const string NotJavaSource = "STAGE-JAVA-0005";

    /// <summary>
    /// STAGE-JAVA-0006: a structured spelling touches text that could change how it parses.
    /// </summary>
    public const string SpellingContext = "STAGE-JAVA-0006";

    /// <summary>
    /// STAGE-JAVA-0007: a Java file violates the declaration and import structure the pack requires.
    /// </summary>
    public const string FileStructure = "STAGE-JAVA-0007";

    /// <summary>
    /// STAGE-JAVA-0008: the same symbol is declared in more than one file.
    /// </summary>
    public const string DuplicateDeclaration = "STAGE-JAVA-0008";

    /// <summary>
    /// STAGE-JAVA-0009: names or packages differ only by ordinal case.
    /// </summary>
    public const string CaseCollision = "STAGE-JAVA-0009";

    /// <summary>
    /// STAGE-JAVA-0010: no short or qualified spelling of a type is provably bound.
    /// </summary>
    public const string Unspellable = "STAGE-JAVA-0010";

    /// <summary>
    /// STAGE-JAVA-0011: an external type has neither a plan declaration nor known member-type facts.
    /// </summary>
    public const string UnknownType = "STAGE-JAVA-0011";

    /// <summary>
    /// STAGE-JAVA-0012: a name used in literal or authored text shadows a type the pack relies on.
    /// </summary>
    public const string LiteralShadowed = "STAGE-JAVA-0012";

    /// <summary>
    /// STAGE-JAVA-0013: a type is declared in the default package.
    /// </summary>
    public const string DefaultPackage = "STAGE-JAVA-0013";

    /// <summary>
    /// STAGE-JAVA-0014: a plan type name equals a plan package name.
    /// </summary>
    public const string PackageTypeClash = "STAGE-JAVA-0014";

    /// <summary>
    /// STAGE-JAVA-0015: authored text violates the output integrity rules.
    /// </summary>
    public const string AuthoredIntegrity = "STAGE-JAVA-0015";

    /// <summary>
    /// STAGE-JAVA-0016: the template primitive has no java@1 spelling.
    /// </summary>
    public const string UnsupportedPrimitive = "STAGE-JAVA-0016";
}
