// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Represents one semantic slice with its application hierarchy.
/// </summary>
/// <param name="Module">The containing module.</param>
/// <param name="FeaturePath">The containing feature path.</param>
/// <param name="Slice">The slice.</param>
/// <param name="Path">The module, feature, and slice display path.</param>
internal sealed record LocatedSemanticSlice(
    SemanticModule Module,
    IReadOnlyList<SemanticFeature> FeaturePath,
    SemanticSlice Slice,
    IReadOnlyList<string> Path)
{
    /// <summary>
    /// Gets the innermost containing feature.
    /// </summary>
    public SemanticFeature Feature => FeaturePath[^1];

    /// <summary>
    /// Determines whether the slice declares an artifact.
    /// </summary>
    /// <param name="artifact">The artifact identity.</param>
    /// <returns><see langword="true"/> when the slice declares the artifact.</returns>
    public bool Declares(SemanticId artifact) => Slice.Events.Any(_ => _.Id == artifact) ||
        Slice.Commands.Any(_ => _.Id == artifact) || Slice.ReadModels.Any(_ => _.Id == artifact) ||
        Slice.Projections.Any(_ => _.Id == artifact) || Slice.Queries.Any(_ => _.Id == artifact) ||
        Slice.Specifications.Any(_ => _.Id == artifact);
}
