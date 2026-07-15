// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Api;

/// <summary>
/// Base type for the runtime command types the engine emits per modeled command. The request body binds into
/// <see cref="Data"/> via <see cref="JsonExtensionDataAttribute"/> so a command can be accepted without a
/// compile-time shape.
/// </summary>
public class DynamicCommand
{
    /// <summary>
    /// Gets the raw command payload captured from the request body.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Data { get; init; } = new Dictionary<string, JsonElement>();
}
