// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies slices whose read artifacts require state-view admission and rendering.
/// </summary>
internal static class SemanticReadSurface
{
    internal static bool IsDeclaredBy(SemanticSlice slice) =>
        !slice.ReadModels.IsEmpty || !slice.Reducers.IsEmpty || !slice.Projections.IsEmpty || !slice.Queries.IsEmpty;
}
