// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hierarchy level addressed by a name-based selection.
/// </summary>
public enum PlanSelectionKind
{
    /// <summary>
    /// A module and all its slices.
    /// </summary>
    Module,

    /// <summary>
    /// A feature, including nested features and their slices.
    /// </summary>
    Feature,

    /// <summary>
    /// One slice.
    /// </summary>
    Slice
}

/// <summary>
/// Addresses one authored hierarchy path using exact ordinal name matching.
/// </summary>
/// <param name="Kind">The hierarchy level.</param>
/// <param name="Path">Module, optional features, and (for a slice) its name.</param>
public sealed record PlanSelectionEntry(PlanSelectionKind Kind, ImmutableArray<string> Path)
{
    /// <summary>
    /// Selects an authored module.
    /// </summary>
    /// <param name="name">The module name.</param>
    /// <returns>The module selection.</returns>
    public static PlanSelectionEntry Module(string name) => new(PlanSelectionKind.Module, [name]);

    /// <summary>
    /// Selects an authored feature or sub-feature.
    /// </summary>
    /// <param name="path">The module and feature names.</param>
    /// <returns>The feature selection.</returns>
    public static PlanSelectionEntry Feature(params string[] path) => new(PlanSelectionKind.Feature, [.. path]);

    /// <summary>
    /// Selects an authored slice.
    /// </summary>
    /// <param name="path">The module, feature names, and slice name.</param>
    /// <returns>The slice selection.</returns>
    public static PlanSelectionEntry Slice(params string[] path) => new(PlanSelectionKind.Slice, [.. path]);
}

/// <summary>
/// Selects a union of slices. Repeated or overlapping entries do not duplicate artifacts.
/// </summary>
/// <param name="Entries">The module, feature, or slice paths; order is immaterial.</param>
public sealed record PlanSelection(ImmutableArray<PlanSelectionEntry> Entries);
