// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits authored command clocks that Arc and Chronicle can preserve exactly.
/// </summary>
public static class SemanticSpecificationClock
{
    /// <summary>
    /// Reads a canonical UTC command clock with millisecond precision.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <param name="instant">The admitted occurrence time.</param>
    /// <returns>Whether the command specification supplies an admitted clock.</returns>
    public static bool TryGet(SemanticSpecification specification, out DateTimeOffset instant)
    {
        instant = default;

        return specification.When is not null && specification.WhenClock is null &&
            specification.GivenClock is { } clock &&
            DateTimeOffset.TryParseExact(clock, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out instant) &&
            instant >= DateTimeOffset.UnixEpoch && instant.Ticks % TimeSpan.TicksPerMillisecond == 0;
    }
}
