// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// What startup reaping decided. It holds only identifiers, ages and reasons.
/// </summary>
/// <param name="Removed">How many stale containers were removed.</param>
/// <param name="Lines">One line per examined container.</param>
public sealed record ContainerReapReport(int Removed, IReadOnlyList<string> Lines);
