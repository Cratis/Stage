// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A named major/minor engine contract.</summary>
/// <param name="Name">Contract name.</param>
/// <param name="Major">Major version.</param>
/// <param name="Minor">Minor version.</param>
public readonly partial record struct ContractVersion(string Name, int Major, int Minor)
{
    internal const string Pattern = @"^[a-z][a-z0-9-]*/v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$";

    [GeneratedRegex(@"^[a-z][a-z0-9-]*/v(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\z", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex IdentityPattern { get; }

    /// <summary>Parses a contract identity without throwing.</summary>
    /// <param name="text">The versioned identity.</param>
    /// <param name="version">The parsed version.</param>
    /// <returns>Whether the identity is valid.</returns>
    public static bool TryParse(string text, out ContractVersion version)
    {
        version = default;
        var match = IdentityPattern.Match(text);
        if (!match.Success || !int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major) || !int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        version = new(text[..text.IndexOf('/', StringComparison.Ordinal)], major, minor);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Major < 0 ? Name : string.Create(CultureInfo.InvariantCulture, $"{Name}/v{Major}.{Minor}");
}
