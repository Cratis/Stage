// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Centralized <see cref="JsonSerializerOptions"/> for deserializing the event model document fed to the engine.
/// </summary>
public static class StageJson
{
    /// <summary>
    /// Gets the JSON options used to read an event model — case-insensitive with string-or-number enum support.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
