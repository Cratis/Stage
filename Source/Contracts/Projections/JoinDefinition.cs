// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a join projection from a source event, joining on a property to another read model.
/// </summary>
/// <param name="On">The property path to join on.</param>
/// <param name="Properties">The property mappings applied from the joined event.</param>
/// <param name="Key">The expression resolving the read-model instance key, or <see langword="null"/> to default.</param>
public record JoinDefinition(
    string On,
    IReadOnlyList<PropertyMapping> Properties,
    string? Key = null);
