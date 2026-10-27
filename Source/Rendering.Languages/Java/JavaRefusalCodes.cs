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
}
