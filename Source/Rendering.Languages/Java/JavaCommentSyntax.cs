// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Supplies Java line-comment syntax and header-line validation for the header writer.
/// Refusal reasons are mapped to header diagnostics by the caller, not by this pack.
/// </summary>
public static class JavaCommentSyntax
{
    /// <summary>
    /// Gets the Java line-comment prefix.
    /// </summary>
    public static string LinePrefix => "//";

    /// <summary>
    /// Validates a header line before comment formatting, without rewriting its bytes.
    /// </summary>
    /// <param name="line">The proposed single header line.</param>
    /// <param name="reason">The refusal reason, or null for a valid line.</param>
    /// <returns>Whether the line contains neither CR/LF nor an eligible Unicode-escape introducer.</returns>
    public static bool TryValidateHeaderLine(string line, out string? reason)
    {
        if (line.Contains('\r', StringComparison.Ordinal) || line.Contains('\n', StringComparison.Ordinal))
        {
            reason = "A Java header line must not contain CR or LF.";
            return false;
        }

        if (JavaUnicodeEscapes.ContainsEligibleEscape(line))
        {
            reason = "A Java header line must not contain an eligible Unicode escape.";
            return false;
        }

        reason = null;
        return true;
    }
}
