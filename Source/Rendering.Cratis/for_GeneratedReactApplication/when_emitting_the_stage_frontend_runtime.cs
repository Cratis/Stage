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
/// A generated application depends on the published <c lang="typescript">@cratis/stage.runtime</c> package at the release that rendered it.
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
    HashSet<string> _optionalPeers = null!;
    Dictionary<string, string> _primeReactPackages = null!;

    void Because()
    {
        _plan = Plan();
        _generatedPackages = Read(Text(_plan, "package.json"), "dependencies", "devDependencies");
        _runtimePeers = Read(File.ReadAllText(RuntimePackage()), "peerDependencies");
        _optionalPeers = OptionalPeers(File.ReadAllText(RuntimePackage()));
        _primeReactPackages = Read(Text(PlanWithPrimeReact(), "package.json"), "dependencies", "devDependencies");
    }

    [Fact] void should_pin_the_runtime_to_the_release_that_rendered_it() => _generatedPackages["@cratis/stage.runtime"].ShouldEqual(ScaffoldPackageVersions.StageRuntime);
    [Fact] void should_pin_the_runtime_exactly() => _generatedPackages["@cratis/stage.runtime"].ShouldNotContain("file:");
    [Fact] void should_not_emit_a_copy_of_the_runtime() => _plan.Artifacts.Where(_ => _.RelativePath.StartsWith(".frontend/stage/", StringComparison.Ordinal) || _.RelativePath == ".frontend/styledPrimeReact.ts").Select(_ => _.RelativePath).ShouldBeEmpty();

    // The runtime declares the PrimeReact peers optional (Cratis/Stage#707): the default application, which is React Aria, does not install them.
    [Fact] void should_pin_every_required_runtime_peer_to_the_version_the_runtime_declares() => Unpinned(_runtimePeers.Where(peer => !_optionalPeers.Contains(peer.Key)), _generatedPackages).ShouldBeEmpty();
    [Fact] void should_not_install_the_optional_runtime_peers_by_default() => _optionalPeers.Where(_generatedPackages.ContainsKey).ShouldBeEmpty();
    [Fact] void should_pin_every_runtime_peer_to_the_version_the_runtime_declares_when_the_application_lists_PrimeReact() => Unpinned(_runtimePeers, _primeReactPackages).ShouldBeEmpty();

    static IEnumerable<string> Unpinned(IEnumerable<KeyValuePair<string, string>> peers, Dictionary<string, string> packages) =>
        peers.Where(peer => !packages.TryGetValue(peer.Key, out var version) || version != peer.Value).Select(peer => $"{peer.Key}: {packages.GetValueOrDefault(peer.Key)} != {peer.Value}");

    static HashSet<string> OptionalPeers(string packageJson)
    {
        using var document = JsonDocument.Parse(packageJson);
        return [.. document.RootElement.GetProperty("peerDependenciesMeta").EnumerateObject()
            .Where(peer => peer.Value.TryGetProperty("optional", out var optional) && optional.GetBoolean())
            .Select(peer => peer.Name)];
    }

    static Dictionary<string, string> Read(string packageJson, params string[] sections)
    {
        using var document = JsonDocument.Parse(packageJson);
        return sections.SelectMany(section => document.RootElement.GetProperty(section).EnumerateObject())
            .ToDictionary(_ => _.Name, _ => _.Value.GetString()!, StringComparer.Ordinal);
    }

    static string RuntimePackage([CallerFilePath] string specification = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specification), "..", "..", "Frontend", "Runtime", "package.json"));
}
