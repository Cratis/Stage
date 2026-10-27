// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Distinguishes unknown metadata from an explicitly known empty or populated type list.
/// The default value is <see cref="Unknown"/>, never <see cref="None"/>. Callers must check
/// <see cref="IsKnown"/> before reading <see cref="Values"/>; unknown metadata cannot be enumerated as empty.
/// </summary>
/// <typeparam name="T">A structured type name or a member type simple name.</typeparam>
public readonly record struct KnownTypes<T>
{
    readonly IReadOnlyList<T>? _values;

    KnownTypes(IReadOnlyList<T> values) => _values = values;

    /// <summary>
    /// Gets unknown metadata, including the default value of this type.
    /// </summary>
    public static KnownTypes<T> Unknown => default;

    /// <summary>
    /// Gets an explicit assertion that there are no types in this list.
    /// </summary>
    public static KnownTypes<T> None { get; } = new(Array.AsReadOnly(Array.Empty<T>()));

    /// <summary>
    /// Gets whether the list is explicitly known, including a known-empty list.
    /// </summary>
    public bool IsKnown => _values is not null;

    /// <summary>
    /// Gets the immutable known list. Unknown is not an empty list.
    /// </summary>
    /// <exception cref="InvalidOperationException">The metadata is unknown.</exception>
    public IReadOnlyList<T> Values => _values ?? throw new InvalidOperationException("Unknown type metadata has no known values.");

    /// <summary>
    /// Captures a known type list without retaining a mutable caller-owned array.
    /// </summary>
    /// <param name="values">The explicitly known values.</param>
    /// <returns>A known, read-only snapshot, including known-none when empty.</returns>
    /// <exception cref="ArgumentNullException">The supplied array is null.</exception>
    public static KnownTypes<T> Of(params T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values.Length == 0 ? None : new(Array.AsReadOnly<T>([.. values]));
    }
}
