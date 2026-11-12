// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Stage.Host.Extensions.Packages;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Reads the <c lang="text">activation</c> lines of the systems in a model, which is how a consumer overrides the default an extension's contract states.
/// </summary>
public static partial class SystemActivationOverrides
{
    [GeneratedRegex(@"^activation\s+(?:(?<always>always)|on\s+demand(?:\s+idle\s+after\s+(?<amount>\d+)\s+(?<unit>seconds?|minutes?|hours?|days?))?)$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ActivationLine { get; }

    /// <summary>
    /// Reads the overrides of every system in a model.
    /// </summary>
    /// <param name="modelPath">The model's folder or one of its files.</param>
    /// <returns>The overrides by system name.</returns>
    public static IReadOnlyDictionary<string, ActivationOverride> Read(string modelPath)
    {
        var overrides = new Dictionary<string, ActivationOverride>(StringComparer.Ordinal);
        foreach (var file in SystemExtensionReferences.PlayFiles(modelPath)) Read(File.ReadLines(file), overrides);
        return overrides;
    }

    /// <summary>
    /// Reads the overrides of the systems in some lines of a model.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="overrides">Where the overrides are collected, by system name.</param>
    public static void Read(IEnumerable<string> lines, IDictionary<string, ActivationOverride> overrides)
    {
        string? system = null;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#')) continue;
            if (!char.IsWhiteSpace(line[0]))
            {
                system = SystemExtensionReferences.SystemName(line);
            }
            else if (system is not null && Parse(line.Trim()) is { } found)
            {
                overrides[system] = found;
            }
        }
    }

    static ActivationOverride? Parse(string line)
    {
        var match = ActivationLine.Match(line);
        if (!match.Success) return null;
        if (match.Groups["always"].Success) return new(true, null, null);
        if (!match.Groups["amount"].Success) return new(false, null, null);
        return int.TryParse(match.Groups["amount"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) && amount > 0 ? new(false, amount, match.Groups["unit"].Value) : null;
    }
}
