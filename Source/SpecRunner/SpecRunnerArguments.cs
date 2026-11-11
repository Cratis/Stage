// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.SpecRunner;

/// <summary>
/// Represents the parsed command-line arguments for the specification runner.
/// </summary>
/// <param name="ModelPath">The path to a Screenplay <c language="csharp">.play</c> file or a directory of files that make up the event model.</param>
/// <param name="OutputPath">The path the result JSON file is written to.</param>
/// <param name="SliceId">An optional slice identifier to limit the run to a single slice.</param>
/// <param name="SpecificationId">An optional specification identifier to limit the run to a single specification.</param>
public record SpecRunnerArguments(string ModelPath, string OutputPath, Guid? SliceId, Guid? SpecificationId)
{
    /// <summary>
    /// Gets the requested engine; structural remains the default.
    /// </summary>
    public string Engine { get; init; } = "structural";

    /// <summary>
    /// Gets whether either structural-only filter was supplied, even if its GUID is invalid.
    /// </summary>
    public bool HasStructuralFilter { get; init; }

    /// <summary>
    /// Gets the semantic specification scope.
    /// </summary>
    public string? Specification { get; init; }

    /// <summary>
    /// Gets additional comma-separated semantic identity scopes.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the optional Screenplay catalog path.
    /// </summary>
    public string? CatalogPath { get; init; }

    /// <summary>
    /// Gets an optional application name to preserve identity across input layouts.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Gets the optional scaffold profile version forwarded to the body worker, or null for the default version 3.
    /// </summary>
    public string? ProfileVersion { get; init; }

    /// <summary>
    /// Parses the supplied command-line arguments.
    /// </summary>
    /// <param name="args">The raw command-line arguments (for example <c language="csharp">--model path --output path</c>).</param>
    /// <returns>The parsed <see cref="SpecRunnerArguments"/>.</returns>
    /// <exception cref="MissingArgument">Thrown when a required argument is missing.</exception>
    /// <exception cref="ArgumentException">Thrown when the body worker profile version is not supported.</exception>
    public static SpecRunnerArguments Parse(string[] args)
    {
        var values = ReadValues(args);
        var modelPath = values.GetValueOrDefault("model") ?? throw new MissingArgument("model");
        var outputPath = values.GetValueOrDefault("output") ?? throw new MissingArgument("output");

        return new SpecRunnerArguments(
            modelPath,
            outputPath,
            ReadIdentifier(values, "slice"),
            ReadIdentifier(values, "spec"))
        {
            Engine = values.GetValueOrDefault("engine") ?? "structural",
            HasStructuralFilter = values.ContainsKey("slice") || values.ContainsKey("spec"),
            Specification = values.GetValueOrDefault("specification"),
            Scope = values.GetValueOrDefault("scope"),
            CatalogPath = values.GetValueOrDefault("catalog"),
            ApplicationName = values.GetValueOrDefault("application"),
            ProfileVersion = ReadProfileVersion(values.GetValueOrDefault("body-worker-profile-version"))
        };
    }

    static Dictionary<string, string> ReadValues(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
            {
                var key = args[index][2..];
                values[key] = key.Equals("scope", StringComparison.OrdinalIgnoreCase) && values.TryGetValue(key, out var previous)
                    ? $"{previous},{args[index + 1]}"
                    : args[index + 1];
                index++;
            }
        }

        return values;
    }

    static Guid? ReadIdentifier(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && Guid.TryParse(value, out var identifier) ? identifier : null;

    static string? ReadProfileVersion(string? value) => value switch
    {
        null => null,
        "3" or "4" => value,
        _ => throw new ArgumentException($"--body-worker-profile-version '{value}' is not a supported scaffold profile version. Supported versions are 3 and 4; omit it for version 3.")
    };
}
