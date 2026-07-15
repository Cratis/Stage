// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.SpecRunner;

/// <summary>
/// Represents the parsed command-line arguments for the specification runner.
/// </summary>
/// <param name="ModelPath">The path to the serialized event model JSON file.</param>
/// <param name="OutputPath">The path the result JSON file is written to.</param>
/// <param name="SliceId">An optional slice identifier to limit the run to a single slice.</param>
/// <param name="SpecificationId">An optional specification identifier to limit the run to a single specification.</param>
public record SpecRunnerArguments(string ModelPath, string OutputPath, Guid? SliceId, Guid? SpecificationId)
{
    /// <summary>
    /// Parses the supplied command-line arguments.
    /// </summary>
    /// <param name="args">The raw command-line arguments (for example <c>--model path --output path</c>).</param>
    /// <returns>The parsed <see cref="SpecRunnerArguments"/>.</returns>
    /// <exception cref="MissingArgument">Thrown when a required argument is missing.</exception>
    public static SpecRunnerArguments Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
            {
                values[args[index][2..]] = args[index + 1];
                index++;
            }
        }

        var modelPath = values.GetValueOrDefault("model") ?? throw new MissingArgument("model");
        var outputPath = values.GetValueOrDefault("output") ?? throw new MissingArgument("output");

        return new SpecRunnerArguments(
            modelPath,
            outputPath,
            values.TryGetValue("slice", out var slice) && Guid.TryParse(slice, out var sliceId) ? sliceId : null,
            values.TryGetValue("spec", out var spec) && Guid.TryParse(spec, out var specId) ? specId : null);
    }
}
