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
    HashSet<string> _optionalPeers = null!;
    Dictionary<string, string> _primeReactPackages = null!;
    Dictionary<string, string> _frontendPackages = null!;
    static readonly string[] _scenePeers = ["@cratis/scene.blueprint.default", "@cratis/scene.components", "@cratis/scene.engine", "@cratis/scene.model", "@cratis/scene.primereact", "@cratis/scene.react"];

    void Because()
    {
        _plan = Plan();
        _generatedPackages = Read(Text(_plan, "package.json"), "dependencies", "devDependencies");
        _runtimePeers = Read(File.ReadAllText(RuntimePackage()), "peerDependencies");
        _frontendPackages = Read(File.ReadAllText(Path.Combine(Path.GetDirectoryName(RuntimePackage())!, "..", "package.json")), "dependencies", "devDependencies");
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

    [Fact] void should_cover_the_complete_scene_peer_roster_in_the_runtime() => SceneNames(_runtimePeers).ShouldEqual(_scenePeers);
    [Fact] void should_cover_the_complete_scene_peer_roster_in_the_frontend() => SceneNames(_frontendPackages).ShouldEqual(_scenePeers);
    [Fact] void should_pin_every_scene_peer_in_both_frontend_manifests_to_the_scaffold_authority() => _scenePeers.Where(name => _runtimePeers[name] != CratisFrontendPackageSet.Current.ScenePackageVersion || _frontendPackages[name] != CratisFrontendPackageSet.Current.ScenePackageVersion).ShouldBeEmpty();
    [Fact] void should_mark_only_the_scene_primereact_peer_optional() => _optionalPeers.Where(IsScene).Order(StringComparer.Ordinal).ShouldEqual(["@cratis/scene.primereact"]);
    [Fact] void should_install_exactly_the_required_scene_peers_by_default() => SceneNames(_generatedPackages).ShouldEqual(_scenePeers.Where(name => !_optionalPeers.Contains(name)));
    [Fact] void should_install_the_complete_scene_peer_roster_with_primereact() => SceneNames(_primeReactPackages).ShouldEqual(_scenePeers);
    [Fact] void should_declare_optional_metadata_only_for_real_runtime_peers() => _optionalPeers.Where(name => !_runtimePeers.ContainsKey(name)).ShouldBeEmpty();

    static bool IsScene(string name) => name.StartsWith("@cratis/scene.", StringComparison.Ordinal);
    static IEnumerable<string> SceneNames(Dictionary<string, string> packages) => packages.Keys.Where(IsScene).Order(StringComparer.Ordinal);

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
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specification)!, "..", "..", "Frontend", "Runtime", "package.json"));
}
