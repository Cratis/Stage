// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Loads an <see cref="EventModel"/> from a JSON file fed to the engine at startup.
/// </summary>
public static class EventModelLoader
{
    /// <summary>
    /// Loads and deserializes the event model from the given file path.
    /// </summary>
    /// <param name="path">The path to the event model JSON file.</param>
    /// <returns>The deserialized <see cref="EventModel"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the file is missing or cannot be deserialized into a model.</exception>
    public static async Task<EventModel> LoadAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidEventModel(path);
        }

        var json = await File.ReadAllTextAsync(path);

        EventModel? model;
        try
        {
            model = JsonSerializer.Deserialize<EventModel>(json, StageJson.Options);
        }
        catch (JsonException)
        {
            throw new InvalidEventModel(path);
        }

        return model ?? throw new InvalidEventModel(path);
    }
}
