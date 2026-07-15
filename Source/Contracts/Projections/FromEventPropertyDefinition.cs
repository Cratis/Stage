// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents mapping a single event property (for example to derive a child collection key).
/// </summary>
/// <param name="EventType">The event type name.</param>
/// <param name="Expression">The property expression.</param>
public record FromEventPropertyDefinition(
    string EventType,
    string Expression);
