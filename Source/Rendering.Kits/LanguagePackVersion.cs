// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>An exact language pack identity.</summary>
/// <param name="Id">Pack identifier.</param>
/// <param name="Version">Exact semantic version.</param>
public readonly partial record struct LanguagePackVersion(string Id, string Version)
{
    internal const string SemverPattern = @"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-([0-9A-Za-z-]+)(\.[0-9A-Za-z-]+)*)?";
    internal const string Pattern = "^[a-z][a-z0-9-]*@" + SemverPattern + "$";

    [GeneratedRegex("^[a-z][a-z0-9-]*@" + SemverPattern + @"\z", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex PackPattern { get; }

    [GeneratedRegex("^" + SemverPattern + @"\z", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex ExactPattern { get; }

    /// <summary>Parses an exact pack binding without accepting ranges.</summary>
    /// <param name="text">Pack identity.</param>
    /// <param name="version">Parsed identity.</param>
    /// <returns>Whether the identity is valid.</returns>
    public static bool TryParse(string text, out LanguagePackVersion version)
    {
        version = default;
        if (!PackPattern.IsMatch(text))
        {
            return false;
        }

        var separator = text.IndexOf('@', StringComparison.Ordinal);
        var exact = text[(separator + 1)..];
        if (!IsExactVersion(exact))
        {
            return false;
        }

        version = new(text[..separator], exact);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Id.Length == 0 ? Version : $"{Id}@{Version}";

    internal static bool IsExactVersion(string text)
    {
        if (!ExactPattern.IsMatch(text))
        {
            return false;
        }

        var separator = text.IndexOf('-', StringComparison.Ordinal);
        return separator < 0 || text[(separator + 1)..].Split('.').All(part => !part.All(char.IsAsciiDigit) || part.Length == 1 || part[0] != '0');
    }
}
