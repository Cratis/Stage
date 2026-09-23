// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when a versioned projection event type cannot be represented by the runtime bridge.
/// </summary>
/// <param name="eventType">The authored event type.</param>
public sealed class UnsupportedProjectionEventType(string eventType)
    : Exception($"Projection event type '{eventType}' cannot be converted to a Chronicle event type.");
