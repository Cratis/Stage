// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Represents the name of the Chronicle event store a play session runs against, generated per session by the host.
/// </summary>
/// <param name="Value">The underlying name.</param>
public record StageEventStoreName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset event store name.
    /// </summary>
    public static readonly StageEventStoreName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a string to a <see cref="StageEventStoreName"/>.
    /// </summary>
    /// <param name="value">The name to convert.</param>
    public static implicit operator StageEventStoreName(string value) => new(value);
}
