// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents removing a joined child when an event occurs.
/// </summary>
/// <param name="Key">The expression resolving the key of the joined child to remove.</param>
public record RemovedWithJoinDefinition(
    string Key);
