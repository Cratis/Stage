// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Represents one event, built from the model and a command payload, ready to be appended.
/// </summary>
/// <param name="EventType">The name of the event type, which is also its Chronicle event type identifier.</param>
/// <param name="Content">The event payload.</param>
/// <param name="Tags">The tags to append the event with.</param>
public record ProducedEventPayload(string EventType, JsonObject Content, IReadOnlyList<string> Tags);
