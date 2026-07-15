// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Reads and writes the specification run results file the Stage specification runner produces — the file
/// consumers (Studio, the CLI) pick up from the runner's mounted output folder.
/// </summary>
public static class SpecificationRunResultsFile
{
    /// <summary>
    /// The well-known file name of a specification run results file.
    /// </summary>
    public const string FileName = "results.json";

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used for the results file — camelCase properties, enums as
    /// strings and indented output.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes the results to their file representation.
    /// </summary>
    /// <param name="results">The results to serialize.</param>
    /// <returns>The JSON content for the results file.</returns>
    public static string Write(SpecificationRunResults results) => JsonSerializer.Serialize(results, SerializerOptions);

    /// <summary>
    /// Writes the results to a file at the given path, creating the folder when needed.
    /// </summary>
    /// <param name="results">The results to write.</param>
    /// <param name="path">The path of the file to write.</param>
    /// <returns>Awaitable task.</returns>
    public static Task WriteToFile(SpecificationRunResults results, string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        return File.WriteAllTextAsync(path, Write(results));
    }

    /// <summary>
    /// Deserializes results from their file representation.
    /// </summary>
    /// <param name="json">The JSON content to deserialize.</param>
    /// <returns>The deserialized <see cref="SpecificationRunResults"/>, or <see langword="null"/> when the content is empty.</returns>
    public static SpecificationRunResults? Read(string json) => JsonSerializer.Deserialize<SpecificationRunResults>(json, SerializerOptions);

    /// <summary>
    /// Reads a results file from the given path.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The deserialized <see cref="SpecificationRunResults"/>, or <see langword="null"/> when the file holds no results.</returns>
    public static async Task<SpecificationRunResults?> ReadFromFile(string path) => Read(await File.ReadAllTextAsync(path));
}
