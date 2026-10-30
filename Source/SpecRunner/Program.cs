// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Running;
using Cratis.Stage.Specifications;
using Cratis.Stage.SpecRunner;

return await Run(args, Console.Out, Console.Error);

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
    public static Task<int> Run(string[] args, TextWriter output, TextWriter error) =>
        Run(args, output, error, EventModelLoader.LoadFromPathAsync);

    internal static async Task<int> Run(string[] args, TextWriter output, TextWriter error, Func<string, Task<EventModel>> load)
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

        if (string.Equals(arguments.Engine, "semantic", StringComparison.OrdinalIgnoreCase))
        {
            return await RunSemantic(arguments, args, output, error);
        }

        if (!string.Equals(arguments.Engine, "structural", StringComparison.OrdinalIgnoreCase))
        {
            await error.WriteLineAsync($"Unknown engine '{arguments.Engine}'.");
            return 2;
        }

        await error.WriteLineAsync("The structural specification engine is deprecated; use --engine semantic. It will be removed in the next major version.");

        EventModel model;
        try
        {
            model = await load(arguments.ModelPath);
        }
        catch (InvalidEventModel exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }
        catch (Exception exception) when (exception is UnsupportedEventSourceRoutes or UnsupportedPublicEvents or UnsupportedForeignEvents or UnsupportedTranslationDirection or UnsupportedReactionIdentity or UnsupportedObserverFilters or UnsupportedGenerationStrategy or UnsupportedValidationRule)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }

        // The default remains structural in this minor version for existing results.json consumers.
#pragma warning disable CS0618 // Compatibility path until the next major version removes the structural engine.
        var runner = new SpecificationRunner();
        var results = runner.Run(model, arguments.SliceId, arguments.SpecificationId);
#pragma warning restore CS0618
        await SpecificationRunResultsFile.WriteToFile(results, arguments.OutputPath);
        await output.WriteLineAsync($"Ran {results.Results.Count} specification(s) for event model '{model.Name}'. Results written to {arguments.OutputPath}.");

        return 0;
    }

    internal static BodyWorkerOptions? ParseBodyWorkerOptions(string[] args) => BodyWorkerOptions.FromArguments(args);

    static async Task<SemanticBodyExecution?> CompileBodies(LoadedSemanticModel loaded, SpecRunnerArguments arguments, string[] args)
    {
        var options = ParseBodyWorkerOptions(args);
        if (options is null || loaded.ImplementationRequirements.Length == 0) return null;
        await using var worker = new BodyWorkerSupervisor(options);
        var manifest = await worker.CompileAsync(arguments.ModelPath, arguments.CatalogPath, loaded.Model.Application.Name);
        return SemanticBodyExecution.From(loaded, manifest, () => new BodyWorkerSupervisor(options, manifest));
    }

    static async Task<int> RunSemantic(SpecRunnerArguments arguments, string[] args, TextWriter output, TextWriter error)
    {
        if (arguments.HasStructuralFilter)
        {
            await error.WriteLineAsync("--slice and --spec are structural-only; use --scope or --specification with --engine semantic.");
            return 2;
        }

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
            await using var bodies = await CompileBodies(loaded, arguments, args);
            var result = await new SemanticSpecificationExecutor().Run(loaded.Plan, new SemanticSpecificationSelection([.. scopes]), new SemanticSpecificationRunOptions
            {
                SpecificationOrigins = loaded.SpecificationOrigins,
                BodyExecution = bodies
            });
            await SemanticSpecificationRunReportFile.WriteToFile(result, arguments.OutputPath);
            await output.WriteLineAsync($"Ran {result.Results.Count} semantic specification(s). Results written to {arguments.OutputPath}.");
            return 0;
        }
        catch (Exception exception) when (exception is InvalidSemanticModel or InvalidSemanticContract or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }
    }
}
