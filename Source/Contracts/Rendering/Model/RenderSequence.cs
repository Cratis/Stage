// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering.Model;

// Immutable member sequences compare by content, rather than ImmutableArray's backing array identity.
internal sealed class RenderSequence<T>(IEnumerable<T> values) : IReadOnlyList<T>, IEquatable<RenderSequence<T>>
{
    readonly ImmutableArray<T> _values = [.. values];

    public int Count => _values.Length;
    public T this[int index] => _values[index];

    public bool Equals(RenderSequence<T>? other) => other is not null && _values.SequenceEqual(other._values);
    public override bool Equals(object? obj) => obj is RenderSequence<T> other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (var value in _values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
