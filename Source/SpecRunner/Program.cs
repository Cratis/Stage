// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Running;
using Cratis.Stage.Specifications;
using Cratis.Stage.SpecRunner;

return await Program.Run(args, Console.Out, Console.Error);

/// <summary>
/// Runs modeled specifications from a Screenplay file or folder.
/// </summary>
public static partial class Program
{
    /// <summary>
    /// Parses input, loads the model, runs its specifications and writes completed results.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="output">The standard output stream.</param>
    /// <param name="error">The standard error stream.</param>
    /// <returns>Two for missing arguments, one for invalid model input, or zero for a completed run.</returns>
    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error)
    {
        // Preserve the command-line application's deterministic output culture.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        SpecRunnerArguments arguments;
        try
        {
            arguments = SpecRunnerArguments.Parse(args);
        }
        catch (MissingArgument exception)
        {
            await error.WriteLineAsync(exception.Message);
            await error.WriteLineAsync("Usage: Cratis.Stage.SpecRunner --model <play-file-or-directory> --output <results.json> [--slice <guid>] [--spec <guid>]");
            return 2;
        }

        if (arguments.Engine == "semantic")
        {
            return await RunSemantic(arguments, output, error);
        }

        if (arguments.Engine != "structural")
        {
            await error.WriteLineAsync($"Unknown engine '{arguments.Engine}'.");
            return 2;
        }

        EventModel model;
        try
        {
            model = await EventModelLoader.LoadFromPathAsync(arguments.ModelPath);
        }
        catch (InvalidEventModel exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }

        var runner = new SpecificationRunner();
        var results = runner.Run(model, arguments.SliceId, arguments.SpecificationId);
        await SpecificationRunResultsFile.WriteToFile(results, arguments.OutputPath);
        await output.WriteLineAsync($"Ran {results.Results.Count} specification(s) for event model '{model.Name}'. Results written to {arguments.OutputPath}.");

        return 0;
    }

    static async Task<int> RunSemantic(SpecRunnerArguments arguments, TextWriter output, TextWriter error)
    {
        var scopes = new List<SemanticId>();
        foreach (var scope in new[] { arguments.Specification }.Concat((arguments.Scope ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)))
        {
            if (scope is null) continue;
            if (!SemanticId.TryParse(scope, out var id))
            {
                await error.WriteLineAsync($"Invalid semantic identity '{scope}'.");
                return 2;
            }

            scopes.Add(id);
        }

        try
        {
            var loaded = await SemanticModelLoader.LoadFromPathAsync(arguments.ModelPath, arguments.CatalogPath, arguments.ApplicationName);
            var result = await new SemanticSpecificationExecutor().Run(loaded.Plan, new SemanticSpecificationSelection([.. scopes]), new SemanticSpecificationRunOptions());
            await SemanticSpecificationRunReportFile.WriteToFile(result, arguments.OutputPath);
            await output.WriteLineAsync($"Ran {result.Results.Count} semantic specification(s). Results written to {arguments.OutputPath}.");
            return 0;
        }
        catch (InvalidSemanticModel exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }
    }
}
