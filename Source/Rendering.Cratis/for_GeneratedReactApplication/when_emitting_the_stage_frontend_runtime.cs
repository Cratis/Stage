// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// A generated application depends on the published <c>@cratis/stage.runtime</c> package at the release that rendered it.
/// </summary>
/// <remarks>
/// The parity between a live Stage and a generated application rests on both running the same runtime, compiled
/// against the same packages. The runtime is installed, not copied, so this checks the pin and that the application's
/// own packages satisfy exactly what the runtime declares as peers in this repository's source tree.
/// </remarks>
public class when_emitting_the_stage_frontend_runtime : a_screen_composition_render
{
    ArtifactRenderPlan _plan = null!;
    Dictionary<string, string> _generatedPackages = null!;
    Dictionary<string, string> _runtimePeers = null!;

    void Because()
    {
        _plan = Plan();
        _generatedPackages = Read(Text(_plan, "package.json"), "dependencies", "devDependencies");
        _runtimePeers = Read(File.ReadAllText(RuntimePackage()), "peerDependencies");
    }

    [Fact] void should_pin_the_runtime_to_the_release_that_rendered_it() => _generatedPackages["@cratis/stage.runtime"].ShouldEqual(ScaffoldPackageVersions.StageRuntime);
    [Fact] void should_pin_the_runtime_exactly() => _generatedPackages["@cratis/stage.runtime"].ShouldNotContain("file:");
    [Fact] void should_not_emit_a_copy_of_the_runtime() => _plan.Artifacts.Where(_ => _.RelativePath.StartsWith(".frontend/stage/", StringComparison.Ordinal) || _.RelativePath == ".frontend/styledPrimeReact.ts").Select(_ => _.RelativePath).ShouldBeEmpty();
    [Fact] void should_pin_every_runtime_peer_to_the_version_the_runtime_declares() => _runtimePeers.Where(peer => !_generatedPackages.TryGetValue(peer.Key, out var version) || version != peer.Value).Select(peer => $"{peer.Key}: {_generatedPackages.GetValueOrDefault(peer.Key)} != {peer.Value}").ShouldBeEmpty();

    static Dictionary<string, string> Read(string packageJson, params string[] sections)
    {
        using var document = JsonDocument.Parse(packageJson);
        return sections.SelectMany(section => document.RootElement.GetProperty(section).EnumerateObject())
            .ToDictionary(_ => _.Name, _ => _.Value.GetString()!, StringComparer.Ordinal);
    }

    static string RuntimePackage([CallerFilePath] string specification = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specification)!, "..", "..", "Frontend", "Runtime", "package.json"));
}
