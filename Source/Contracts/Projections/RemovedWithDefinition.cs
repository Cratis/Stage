// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents removing a model instance when an event occurs.
/// </summary>
/// <param name="Key">The expression resolving the key of the instance to remove.</param>
/// <param name="ParentKey">The expression resolving the parent key, or <see langword="null"/> when not a child.</param>
public record RemovedWithDefinition(
    string Key,
    string? ParentKey = null);
