// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents a feature — a grouping of slices within a module. May be nested under a parent feature.
/// </summary>
/// <param name="Id">The unique identifier of the feature.</param>
/// <param name="Name">The name of the feature.</param>
/// <param name="ParentFeatureId">The identifier of the parent feature when nested, or <see langword="null"/> for top-level features.</param>
/// <param name="SubFeatures">The sub-features nested within the feature.</param>
/// <param name="Slices">The slices within the feature.</param>
public record Feature(
    Guid Id,
    string Name,
    Guid? ParentFeatureId,
    IReadOnlyList<Feature> SubFeatures,
    IReadOnlyList<Slice> Slices);
