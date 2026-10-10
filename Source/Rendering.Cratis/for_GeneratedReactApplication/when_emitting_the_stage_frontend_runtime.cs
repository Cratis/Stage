// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// The Stage runtime a generated application is emitted with, compared with the live Stage frontend's sources.
/// </summary>
/// <remarks>
/// The parity between a live Stage and a generated application rests on both running the same runtime, compiled
/// against the same packages. This reads the live frontend from this repository's source tree.
/// </remarks>
public class when_emitting_the_stage_frontend_runtime : a_screen_composition_render
{
    static readonly string[] _sharedPackages =
    [
        "@cratis/arc", "@cratis/arc.react", "@cratis/components", "@cratis/fundamentals",
        "@cratis/scene.blueprint.default", "@cratis/scene.engine", "@cratis/scene.model", "@cratis/scene.primereact", "@cratis/scene.react",
        "@primereact/core", "@primereact/headless", "@primereact/ui", "@primeuix/themes", "primeicons", "primereact",
        "react", "react-dom", "reflect-metadata", "tsyringe", "@types/node", "@types/react", "@types/react-dom", "@vitejs/plugin-react"
    ];

    static readonly string[] _liveOnly = ["main.tsx", "testSetup.ts", "testElements.ts"];

    ArtifactRenderPlan _plan = null!;
    string _frontend = null!;
    JsonElement _generatedPackages;
    JsonElement _livePackages;

    void Because()
    {
        _plan = Plan();
        _frontend = FrontendDirectory();
        _generatedPackages = Dependencies(Text(_plan, "package.json"));
        _livePackages = Dependencies(File.ReadAllText(Path.Combine(_frontend, "package.json")));
    }

    [Fact] void should_emit_every_live_runtime_module() => RuntimeArtifacts().Select(_ => _.Name).ShouldContainOnly(LiveRuntimeModules());
    [Fact] void should_emit_each_module_byte_for_byte() => RuntimeArtifacts().Where(_ => _.Text != Live(_.Name)).Select(_ => _.Name).ShouldBeEmpty();
    [Fact] void should_emit_the_styled_primereact_plugin_byte_for_byte() => Text(_plan, ".frontend/styledPrimeReact.ts").ShouldEqual(File.ReadAllText(Path.Combine(_frontend, "styledPrimeReact.ts")).Replace("\r\n", "\n", StringComparison.Ordinal));
    [Fact] void should_pin_every_shared_package_to_the_live_version() => _sharedPackages.Where(name => Version(_generatedPackages, name) != Version(_livePackages, name)).Select(name => $"{name}: {Version(_generatedPackages, name)} != {Version(_livePackages, name)}").ShouldBeEmpty();

    (string Name, string Text)[] RuntimeArtifacts() => [.. _plan.Artifacts
        .Where(_ => _.RelativePath.StartsWith(".frontend/stage/", StringComparison.Ordinal))
        .Select(_ => (_.RelativePath[".frontend/stage/".Length..], System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())))];

    string[] LiveRuntimeModules() => [.. Directory.GetFiles(Path.Combine(_frontend, "src"))
        .Select(Path.GetFileName)
        .OfType<string>()
        .Where(name => (name.EndsWith(".ts", StringComparison.Ordinal) || name.EndsWith(".tsx", StringComparison.Ordinal) || name.EndsWith(".css", StringComparison.Ordinal)) &&
            !name.Contains(".spec.", StringComparison.Ordinal) && !_liveOnly.Contains(name, StringComparer.Ordinal))];

    string Live(string name) => File.ReadAllText(Path.Combine(_frontend, "src", name)).Replace("\r\n", "\n", StringComparison.Ordinal);

    static JsonElement Dependencies(string packageJson)
    {
        using var document = JsonDocument.Parse(packageJson);
        var all = document.RootElement.GetProperty("dependencies").EnumerateObject()
            .Concat(document.RootElement.GetProperty("devDependencies").EnumerateObject())
            .ToDictionary(_ => _.Name, _ => _.Value.GetString(), StringComparer.Ordinal);
        return JsonSerializer.SerializeToElement(all);
    }

    static string? Version(JsonElement packages, string name) => packages.TryGetProperty(name, out var version) ? version.GetString() : null;

    static string FrontendDirectory([CallerFilePath] string specification = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specification)!, "..", "..", "Frontend"));
}
