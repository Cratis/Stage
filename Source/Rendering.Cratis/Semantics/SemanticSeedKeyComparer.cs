// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Compares seed keys by value: a composite key holds an array, so its record equality is reference-based.
/// </summary>
internal sealed class SemanticSeedKeyComparer : IEqualityComparer<SemanticValue>
{
    internal static readonly SemanticSeedKeyComparer Instance = new();

    /// <inheritdoc/>
    public bool Equals(SemanticValue? x, SemanticValue? y) => x is null || y is null ? x is null && y is null : SameValue(x, y);

    /// <inheritdoc/>
    public int GetHashCode(SemanticValue obj) => 0; // Seed counts are tiny, so one bucket and a linear scan is fine.

    static bool SameValue(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length &&
            a.Properties.All(property => b.Properties.Any(candidate => candidate.TargetProperty == property.TargetProperty && SameValue(property.Value, candidate.Value))),
        (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => SameValue(pair.First, pair.Second)),
        _ => object.Equals(left, right)
    };
}
