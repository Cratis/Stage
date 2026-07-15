// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a single property mapping — pairing a target property path with the source expression that fills it.
/// </summary>
/// <remarks>
/// The <paramref name="Expression"/> is an expression string compatible with Chronicle's projection engine — either a
/// plain event property name, or one of the well-known tokens (for example <c>$eventSourceId</c>, <c>$eventContext.occurred</c>,
/// <c>$value</c>, <c>$add</c>, <c>$increment</c>).
/// </remarks>
/// <param name="Property">The target property path on the read model.</param>
/// <param name="Expression">The source expression that produces the value.</param>
public record PropertyMapping(
    string Property,
    string Expression);
