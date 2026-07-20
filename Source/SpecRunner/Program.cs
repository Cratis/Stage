// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Stage.Contracts;
using Cratis.Stage.Running;
using Cratis.Stage.SpecRunner;

// Force invariant culture for deterministic output.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

SpecRunnerArguments arguments;
try
{
    arguments = SpecRunnerArguments.Parse(args);
}
catch (MissingArgument exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    await Console.Error.WriteLineAsync("Usage: Cratis.Stage.SpecRunner --model <play-directory> --output <results.json> [--slice <guid>] [--spec <guid>]");
    return 2;
}

var model = await EventModelLoader.LoadFromDirectoryAsync(arguments.ModelPath);

var runner = new SpecificationRunner();
var results = runner.Run(model, arguments.SliceId, arguments.SpecificationId);

await SpecificationRunResultsFile.WriteToFile(results, arguments.OutputPath);

await Console.Out.WriteLineAsync($"Ran {results.Results.Count} specification(s) for event model '{model.Name}'. Results written to {arguments.OutputPath}.");

return 0;
