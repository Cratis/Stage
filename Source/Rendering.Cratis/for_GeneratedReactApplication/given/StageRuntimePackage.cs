// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Installs the Stage runtime from this repository's own source into a generated application's build.
/// </summary>
/// <remarks>
/// A generated application pins <c language="json">@cratis/stage.runtime</c> at the Stage release that rendered it, and a
/// Debug build is version 1.0.0, which is not published. The renderer never emits a local path; only this build harness
/// swaps the pin for the packed runtime, so the build runs against the runtime the repository contains.
/// </remarks>
public static class StageRuntimePackage
{
    const string Name = "@cratis/stage.runtime";
    static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(8);
    static readonly Lazy<Task<string>> _tarball = new(Pack);

    /// <summary>
    /// Points a rendered application at the packed runtime instead of the released version it pins.
    /// </summary>
    /// <param name="application">The rendered application directory.</param>
    /// <returns>A task that completes when <c>package.json</c> has been rewritten.</returns>
    public static async Task Install(string application)
    {
        var tarball = await _tarball.Value;
        var path = Path.Combine(application, "package.json");
        var package = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        package["dependencies"]![Name] = $"file:{tarball}";
        File.WriteAllText(path, package.ToJsonString(new() { WriteIndented = true }));
    }

    /// <summary>
    /// Lists every installed copy of the packages that must exist once, because they hold registries and contexts.
    /// </summary>
    /// <param name="application">The application with its packages installed.</param>
    /// <returns>An empty string when each is installed exactly once; otherwise a description of the duplicates.</returns>
    public static async Task<string> SingleInstanceFailure(string application)
    {
        string[] shared = ["@cratis/arc", "@cratis/arc.react", "@cratis/scene.components", "@cratis/scene.engine", "@cratis/scene.model", "@cratis/scene.react", "react"];
        var result = await ApplicationProcess.Run(application, _stepTimeout, "npm", ["ls", "--all", "--parseable", .. shared]);
        var duplicates = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Contains("node_modules", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .GroupBy(line => line[(line.LastIndexOf("node_modules", StringComparison.Ordinal) + "node_modules".Length + 1)..], StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group)}");
        return string.Join(Environment.NewLine, duplicates);
    }

    static async Task<string> Pack()
    {
        var frontend = FrontendDirectory();
        var destination = SpecTemporaryRoot.NewPath("stage-runtime-package");
        Directory.CreateDirectory(destination);
        await Step(frontend, "npm", "ci", "--no-audit", "--no-fund");
        await Step(frontend, "npm", "run", "build", "--workspace", Name);
        var packed = await Step(frontend, "npm", "pack", "--workspace", Name, "--pack-destination", destination);
        return Path.Combine(destination, packed.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[^1]);
    }

    static async Task<ProcessResult> Step(string directory, string fileName, params string[] arguments)
    {
        var result = await ApplicationProcess.Run(directory, _stepTimeout, fileName, arguments);
        return result.ExitCode == 0
            ? result
            : throw new InvalidOperationException($"{fileName} {string.Join(' ', arguments)} failed with exit {result.ExitCode}{Environment.NewLine}{result.Output}");
    }

    static string FrontendDirectory([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "Frontend"));
}
