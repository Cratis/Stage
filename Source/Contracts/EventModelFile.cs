// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Reads and writes serialized <see cref="EventModel"/> files — the input consumers (Studio, the CLI) hand to the
/// Stage host and specification runner. Both sides use this type so the format always agrees.
/// </summary>
public static class EventModelFile
{
    /// <summary>
    /// The well-known file name of a serialized event model.
    /// </summary>
    public const string FileName = "event-model.json";

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used when writing an event model file — the same options
    /// <see cref="StageJson"/> reads with, plus indented output.
    /// </summary>
    public static readonly JsonSerializerOptions WriteOptions = new(StageJson.Options)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an <see cref="EventModel"/> to its file representation.
    /// </summary>
    /// <param name="model">The model to serialize.</param>
    /// <returns>The JSON content for the event model file.</returns>
    public static string Write(EventModel model) => JsonSerializer.Serialize(model, WriteOptions);

    /// <summary>
    /// Writes an <see cref="EventModel"/> to a file at the given path.
    /// </summary>
    /// <param name="model">The model to write.</param>
    /// <param name="path">The path of the file to write.</param>
    /// <returns>Awaitable task.</returns>
    public static Task WriteToFile(EventModel model, string path) => File.WriteAllTextAsync(path, Write(model));
}
