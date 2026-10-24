// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

static class GeneratedSpecificationArguments
{
    internal static string[] Native(string project, string[] arguments)
    {
        var build = new List<string> { "run", "--project", project };
        var runner = new List<string> { "--specifications", "-maxThreads", "3" };
        string? resultName = null;
        var resultDirectory = ".";
        for (var index = 1; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == "--") break;
            if (argument == project || argument == "--nologo" || argument.StartsWith("-m:", StringComparison.Ordinal)) continue;
            switch (argument)
            {
                case "-c" or "--configuration":
                    build.Add(argument);
                    build.Add(arguments[++index]);
                    break;
                case "--no-build" or "--no-restore":
                    build.Add(argument);
                    break;
                case "--filter":
                    runner.Add("-filterVSTest");
                    runner.Add(arguments[++index]);
                    break;
                case "--logger":
                    const string prefix = "trx;LogFileName=";
                    var logger = arguments[++index];
                    if (!logger.StartsWith(prefix, StringComparison.Ordinal)) throw new ArgumentException($"Unsupported generated-specification logger: {logger}");
                    resultName = logger[prefix.Length..];
                    break;
                case "--results-directory":
                    resultDirectory = arguments[++index];
                    break;
                default:
                    throw new ArgumentException($"Unsupported generated-specification argument: {argument}");
            }
        }
        if (resultName is not null) runner.AddRange(["-result-trx", Path.Combine(resultDirectory, resultName)]);
        return [.. build, "--", .. runner];
    }
}
