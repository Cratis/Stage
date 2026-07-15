// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents how a set of properties is mapped from a single source event.
/// </summary>
/// <param name="Properties">The property mappings applied for the event.</param>
/// <param name="Key">The expression resolving the read-model instance key, or <see langword="null"/> to default to the event source id.</param>
/// <param name="ParentKey">The expression resolving the parent key for a child relationship, or <see langword="null"/> when not a child.</param>
public record FromDefinition(
    IReadOnlyList<PropertyMapping> Properties,
    string? Key = null,
    string? ParentKey = null);
