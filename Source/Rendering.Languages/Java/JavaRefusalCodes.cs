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
    /// STAGE-JAVA-0003: a raw field, literal stream, or its JLS 3.3 translation contains a private-use code point.
    /// </summary>
    public const string PrivateUseCodePoint = "STAGE-JAVA-0003";

    /// <summary>
    /// STAGE-JAVA-0004: a raw field, literal stream, or its JLS 3.3 translation contains a lone UTF-16 surrogate.
    /// </summary>
    public const string LoneSurrogate = "STAGE-JAVA-0004";

    /// <summary>
    /// STAGE-JAVA-0005: a gap or native segment has a context other than type, member, or statement.
    /// </summary>
    public const string UnsupportedContext = "STAGE-JAVA-0005";

    /// <summary>
    /// STAGE-JAVA-0006: a literal stream ends in an eligible raw backslash before a segment with unresolved output.
    /// </summary>
    public const string UnsafeLiteralBoundary = "STAGE-JAVA-0006";
}
