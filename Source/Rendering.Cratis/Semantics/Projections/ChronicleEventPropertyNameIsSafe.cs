// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Prevents event property paths from being interpreted as Chronicle expression literals.
/// </summary>
internal static class ChronicleEventPropertyNameIsSafe
{
    internal static bool Check(string name) =>
        !name.Contains('$') &&
        !name.Contains('.') &&
        !name.Contains('[') &&
        !name.Contains(']') &&
        !name.StartsWith('"') &&
        !string.Equals(name, "true", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(name, "false", StringComparison.OrdinalIgnoreCase) &&
        !long.TryParse(name, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _) &&
        !decimal.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
        !(double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number));
}
