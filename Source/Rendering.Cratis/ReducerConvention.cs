// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents a resolved reducer convention: the application default and the read models migrated to self-reduction.
/// </summary>
/// <param name="SelfDefault">Whether read models without an override render self-reducing.</param>
/// <param name="Overrides">The migrated read models by semantic identity.</param>
internal sealed record ReducerConvention(bool SelfDefault, ImmutableDictionary<SemanticId, ReducerOverride> Overrides)
{
    internal static readonly ReducerConvention Classic = new(false, []);

    /// <summary>
    /// Gets whether eligible From-only scoped projections render as model-bound read models.
    /// </summary>
    internal bool ModelBoundProjections { get; init; }

    internal bool SelfReduces(SemanticId readModel) => SelfDefault || Overrides.ContainsKey(readModel);

    internal string? PinnedObserverId(SemanticId readModel) => Overrides.TryGetValue(readModel, out var pinned) ? pinned.ObserverId : null;
}

/// <summary>
/// Represents one read model migrated from a classic reducer to self-reduction.
/// </summary>
/// <param name="ObserverId">The previous classic reducer's observer identity, kept on every render.</param>
/// <param name="PreviousPath">The application-root-relative path of the removed classic reducer file.</param>
internal sealed record ReducerOverride(string ObserverId, string PreviousPath);
