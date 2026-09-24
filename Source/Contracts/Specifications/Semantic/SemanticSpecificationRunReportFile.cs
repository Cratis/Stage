// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts.Specifications.Semantic;

/// <summary>
/// Reads and writes versioned semantic specification results without changing the structural result file contract.
/// </summary>
public static class SemanticSpecificationRunReportFile
{
    /// <summary>
    /// The default results file name.
    /// </summary>
    public const string FileName = "results.json";

    /// <summary>
    /// JSON settings for the semantic results contract.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes a report.
    /// </summary>
    /// <param name="report">The report to serialize.</param>
    /// <returns>The JSON content.</returns>
    public static string Write(SemanticSpecificationRunReport report) => JsonSerializer.Serialize(report, SerializerOptions);

    /// <summary>
    /// Deserializes a report.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The report, or null for an empty JSON value.</returns>
    public static SemanticSpecificationRunReport? Read(string json) => JsonSerializer.Deserialize<SemanticSpecificationRunReport>(json, SerializerOptions);

    /// <summary>
    /// Writes a report to a file, creating its directory if needed.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="path">The file path.</param>
    /// <returns>The write operation.</returns>
    public static Task WriteToFile(SemanticSpecificationRunReport report, string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        return File.WriteAllTextAsync(path, Write(report));
    }

    /// <summary>
    /// Reads a report from a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The report, or null for an empty JSON value.</returns>
    public static async Task<SemanticSpecificationRunReport?> ReadFromFile(string path) => Read(await File.ReadAllTextAsync(path));
}
