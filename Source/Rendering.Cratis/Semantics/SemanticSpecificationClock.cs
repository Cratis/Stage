// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Validates authored occurrence clocks without broadening the generated command-clock contract.
/// </summary>
internal static class SemanticSpecificationClock
{
    /// <summary>
    /// Reads a generated command scenario's canonical UTC clock at or after the Unix epoch with millisecond precision.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <param name="instant">The admitted occurrence time.</param>
    /// <returns>Whether the command specification supplies an admitted clock.</returns>
    internal static bool TryGet(SemanticSpecification specification, out DateTimeOffset instant)
    {
        instant = default;

        return specification.When is not null && specification.WhenClock is null && TryParse(specification.GivenClock, out instant);
    }

    /// <summary>
    /// Reads the authored occurrence clock for an independent command, append or redelivery scenario.
    /// </summary>
    /// <param name="specification">The specification with exactly one supported action and no capture fixtures.</param>
    /// <param name="instant">The canonical UTC occurrence, at or after the Unix epoch with millisecond precision.</param>
    /// <returns>Whether the scenario supplies an admitted clock; absence never supplies the current time.</returns>
    internal static bool TryGetOccurrence(SemanticSpecification specification, out DateTimeOffset instant)
    {
        instant = default;
        var actions = (specification.When is not null ? 1 : 0) + (specification.WhenAppended is not null ? 1 : 0) + (specification.WhenRedelivered is not null ? 1 : 0);

        return actions == 1 && specification.WhenClock is null && specification.WhenTrigger is null &&
            specification.WhenCapture is null && specification.GivenCaptures.IsDefaultOrEmpty && TryParse(specification.GivenClock, out instant);
    }

    static bool TryParse(string? clock, out DateTimeOffset instant)
    {
        instant = default;

        return clock is not null &&
            DateTimeOffset.TryParseExact(
                clock,
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out instant) &&
            instant >= DateTimeOffset.UnixEpoch && instant.Ticks % TimeSpan.TicksPerMillisecond == 0;
    }
}
